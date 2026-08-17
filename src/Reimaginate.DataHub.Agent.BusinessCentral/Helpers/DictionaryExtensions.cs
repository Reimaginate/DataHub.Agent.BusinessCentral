namespace Reimaginate.DataHub.Agent.BusinessCentral.Helpers
{
    public static class DictionaryExtensions
    {
        public static Dictionary<string, object> Merge(this Dictionary<string, object> dictionary, Dictionary<string, object> dictionaryToMerge)
        {
            foreach (var entry in dictionaryToMerge)
            {
                dictionary[entry.Key] = entry.Value;
            }

            return dictionary;
        }
    }
}
