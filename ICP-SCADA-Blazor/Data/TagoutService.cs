using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace ICP_SCADA_Blazor.Data
{
    public class TagoutService
    {
        private int AutoUnselectTimming = 10; //default: ten seconds
        private List<TagoutList> TagoutLists = new List<TagoutList>();
        private List<VisibleMapping> VisibleMappings = new List<VisibleMapping>();
        private List<OtherMapping> OtherMappings = new List<OtherMapping>();
        private readonly IConfigurationRoot _config;
        public TagoutService(IConfigurationRoot config)
        {
            _config = config;
            UpdateVisibleConfigurations();
        }
        
        public Task<List<TagoutList>> GetTagoutListAsync()
        {
            return Task.FromResult(TagoutLists);
        }
        public Task<int> GetAutoUnselectTimming()
        {
            return Task.FromResult(AutoUnselectTimming);
        }
        public void ModifyAutoUnselectTimming(int timming)
        {
            AutoUnselectTimming = timming;
        }
        public void AddTagoutList(TagoutList tagoutItem)
        {
            TagoutLists.Add(tagoutItem);
        }
        public void RemoveTagoutList(int id, string item)
        {
            var itemToRemove = TagoutLists.Single(r => r.Index == id && r.item == item);
            TagoutLists.Remove(itemToRemove);
        }
        public int TagoutQuantity()
        {
            return TagoutLists.Count();
        }
        private void UpdateVisibleConfigurations()
        {
            VisibleMappings = _config.GetSection("Tags").Get<List<VisibleMapping>>();
            OtherMappings = _config.GetSection("Others").Get<List<OtherMapping>>();
        }
        public string VisibleTranslate(string tag)
        {
            string result = (from v in VisibleMappings
                         where v.TagName == tag
                         select v.VisibleString).FirstOrDefault();
            if (String.IsNullOrEmpty(result))
            {
                return tag;
            }
            return result;
        }
        public string GetTrueString(string tag)
        {
            string result = (from v in VisibleMappings
                             where v.TagName == tag
                             select v.State_TRUE).FirstOrDefault();
            if (String.IsNullOrEmpty(result))
            {
                return @"True";
            }
            return result;
        }
        public string GetFalseString(string tag)
        {
            string result = (from v in VisibleMappings
                             where v.TagName == tag
                             select v.State_FALSE).FirstOrDefault();
            if (String.IsNullOrEmpty(result))
            {
                return @"False";
            }
            return result;
        }
        public int GetLastIndex(string tag)
        {
            int result = (from i in TagoutLists
                             where i.item == tag || i.item == @"Global"
                             select i.Index).FirstOrDefault();
            return result;
        }
        public OtherMapping GetOtherMappingTag(string coTag)
        {
            var result = (from i in OtherMappings
                          where i.ControlTag == coTag
                          select i).FirstOrDefault();
            return result;
        }
        public bool isTagouted(string coTag)
        {
            int result = (from i in TagoutLists
                          where i.ControlTag == coTag
                          select i).Count();
            if(result >= 1)
            {
                return true;
            }
            return false;
        }
        public void RemoveSpecialTag(string coTag)
        {
            var itemToRemove = TagoutLists.FindAll(r => r.ControlTag == coTag);
            foreach(var item in itemToRemove) TagoutLists.Remove(item);
        }
    }
}
