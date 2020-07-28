using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using IEC61850.Server;
using IEC61850.Common;
using Microsoft.Extensions.DependencyInjection;
using ICP_SCADA_Blazor.Data;

namespace ICP_SCADA_Blazor
{
	class DataObjectMapping
	{
		public DataObject dataObject { get; set; }
		public string tagName { get; set; }
		public DataObjectMapping(DataObject d, string t)
		{
			dataObject = d;
			tagName = t;
		}
	}
    public class s61850: BackgroundService, IHostedService
	{
		private IedModel iedModel;
		private IedServerConfig config;
		private IedServer iedServer;
		private readonly ILogger<s61850> _logger;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly IHubContext<SignalRHub, IHub> _hub;
		private List<DataObjectMapping> DoMapping = new List<DataObjectMapping>();


		public s61850(ILogger<s61850> logger, IServiceScopeFactory scopeFactory, IHubContext<SignalRHub, IHub> hub)
		{
			_logger = logger;
			_scopeFactory = scopeFactory;
			_hub = hub;
			iedModel = ConfigFileParser.CreateModelFromConfigFile("model.cfg");
			if (iedModel == null)
			{
				_logger.LogError("SYSERR: No Valid DataModel Found!");
				return;
			}

			config = new IedServerConfig();
			config.ReportBufferSize = 100000;

			iedServer = new IedServer(iedModel, config);
		}

		public async Task AddTagout(string tag)
		{
			using (var scope = _scopeFactory.CreateScope())
			{
				TagoutService ts = scope.ServiceProvider.GetService<TagoutService>();

				var oMapping = ts.GetOtherMappingTag(tag);
				if (!ts.isTagouted(tag) && oMapping != null)
				{
					foreach(var tagName in oMapping.AssociateTag)
					{
						TagoutList tagoutList = new TagoutList();
						tagoutList.Index = 999;
						tagoutList.item = tagName;
						tagoutList.VisibleString = oMapping.VisibleString;
						tagoutList.Datetime = DateTime.Now;
						tagoutList.Reason = oMapping.Reason;
						tagoutList.Comment = oMapping.Comment;
						tagoutList.Owner = @"System";
						tagoutList.Special = true;
						tagoutList.ControlTag = tag;
						ts.AddTagoutList(tagoutList);
					}
				}
				await _hub.Clients.All.ReceivedUpdate($"Tagout Info: {oMapping.Comment}");
			}
		}
		public async Task RemoveTagout(string tag)
		{
			using (var scope = _scopeFactory.CreateScope())
			{
				TagoutService ts = scope.ServiceProvider.GetService<TagoutService>();
				if (ts.isTagouted(tag))
				{
					ts.RemoveSpecialTag(tag);
				}
			}
			await _hub.Clients.All.ReceivedUpdate($"");
		}

		public void ModifyFloatValue(string ObjRef, string value)
		{
			DataObject DataObj = (DataObject)iedModel.GetModelNodeByShortObjectReference(ObjRef);

			DataAttribute DataObj_F = (DataAttribute)DataObj.GetChild("mag.f");
			DataAttribute DataObj_T = (DataAttribute)DataObj.GetChild("t");

			iedServer.UpdateFloatAttributeValue(DataObj_F, float.Parse(value));
			iedServer.UpdateTimestampAttributeValue(DataObj_T, new Timestamp(DateTime.Now));
		}

		public void ModifySpsValue(string ObjRef, bool value)
		{
			DataObject DataObj = (DataObject)iedModel.GetModelNodeByShortObjectReference(ObjRef);

			DataAttribute DataObj_ST = (DataAttribute)DataObj.GetChild("stVal");
			DataAttribute DataObj_T = (DataAttribute)DataObj.GetChild("t");

			iedServer.UpdateBooleanAttributeValue(DataObj_ST, value);
			iedServer.UpdateTimestampAttributeValue(DataObj_T, new Timestamp(DateTime.Now));
		}
		public bool ReadSpsValue(string ObjRef)
		{
			DataObject DataObj = (DataObject)iedModel.GetModelNodeByShortObjectReference(ObjRef);

			DataAttribute DataObj_ST = (DataAttribute)DataObj.GetChild("stVal");

			return iedServer.GetAttributeValue(DataObj_ST).GetBoolean();
		}

		public void ModifyDpsValue(string ObjRef, byte value)
		{
			DataObject DataObj = (DataObject)iedModel.GetModelNodeByShortObjectReference(ObjRef);

			DataAttribute DataObj_ST = (DataAttribute)DataObj.GetChild("stVal");
			DataAttribute DataObj_T = (DataAttribute)DataObj.GetChild("t");

			iedServer.UpdateAttributeValue(DataObj_ST, new MmsValue(value));
			iedServer.UpdateTimestampAttributeValue(DataObj_T, new Timestamp(DateTime.Now));
		}

		public async Task SetControlListener(string ObjRef)
		{
			_logger.LogInformation($"SYSLOG: Set Control Listener: {ObjRef}");
			DataObject ControlPoint = (DataObject)iedModel.GetModelNodeByShortObjectReference(ObjRef);
			DataObjectMapping tmp = new DataObjectMapping(ControlPoint, ObjRef);
			DoMapping.Add(tmp);
			iedServer.SetCheckHandler(ControlPoint, delegate (ControlAction action, object parameter, MmsValue ctlVal, bool test, bool interlockCheck) {

				_logger.LogInformation($"SYSLOG: Received binary control command:" +
					$"\n\tctlNum: {action.GetCtlNum()}" +
					$"\n\texecution-time: {action.GetControlTimeAsDataTimeOffset().ToString()}");

				return CheckHandlerResult.ACCEPTED;
			}, null);

			iedServer.SetControlHandler(ControlPoint, delegate (ControlAction action, object parameter, MmsValue ctlVal, bool test) {
				bool val = ctlVal.GetBoolean();

				string result = (from v in DoMapping
								 where v.dataObject == ControlPoint
								 select v.tagName).FirstOrDefault();
				result = result.Split('.').Last();

				if (val)
				{
					AddTagout(result);
					_logger.LogInformation("CTRL: Execute binary control command: ON");
				}
				else
				{
					RemoveTagout(result);
					_logger.LogInformation("CTRL: Execute binary control command: OFF");
				}
				return ControlHandlerResult.OK;
			}, null);
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await Task.Factory.StartNew(() => {
				iedServer.Start(102);
				for (int i = 1; i <= 30; i++)
				{
					try
					{
						SetControlListener($"ICPSI/STGGIO1.SPCSO{i}");
					}
					catch(Exception ex)
					{
						_logger.LogWarning(ex.Message);
					}
				}
				_logger.LogInformation("SYSLOG: Iec61850 Server is Listening on port 102.");
				GC.Collect();

			}, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}
	}
}