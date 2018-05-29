using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace JsonTests.Data
{
    /// <summary>
    /// DyanamicDocument extends a .net DynamicObject by adding properties and 
    /// methods required for it to be stored in a DocumentDb repository.
    /// 
    /// The class supports (de)serialisation of dyanmically set properties.
    /// 
    /// Copyright (c) 2017 Biotware Ltd.
    /// </summary>
    public abstract class DynamicDocument : DynamicObject
    {
        /// <summary>
        /// Error result message returned when attempting to access
        /// an invalid (non-existent) property
        /// </summary>
        public const string PROPERTY_INVALID = "Invalid Property";

        /// <summary>
        /// Property that holds a POSIX timestamp, in seconds, representing 
        /// when the object was initially saved to the underlying data store.
        /// 
        /// This should be stored as UTC
        /// </summary>
        [JsonProperty(PropertyName = "_created")]
        public long CreatedAt { get; set; }

        protected DynamicDocument() { }

        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        #region DynamicObject Overrides
        /// <summary>
        /// Attempt to lookup a property from the internal dictionary.
        /// </summary>
        /// <param name="binder">Property details of the member to find</param>
        /// <param name="result">out parameter for the value that is returned from the property dictionary.</param>
        /// <returns>Returns true if property found, else false.  result will contain error message in the event of a failure</returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (this.Properties.ContainsKey(binder.Name))
            {
                result = this.Properties[binder.Name];
                return true;
            }
            else
            {
                result = DynamicDocument.PROPERTY_INVALID;
                return false;
            }
        }

        /// <summary>
        /// Assigns a value to a given property
        /// </summary>
        /// <param name="binder">Property details of the member to bind</param>
        /// <param name="value">Value to bind</param>
        /// <returns>true</returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this.Properties[binder.Name] = value;

            return true;
        }

        /// <summary>
        /// Get a list of the currently declared dynamic properties
        /// </summary>
        /// <returns>Enumberable list of property names.</returns>
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return this.Properties.Keys;
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">Property name</param>
        /// <returns>Value of the property if it exists, or the default value of the type if it doesn't</returns>
        public object this[string key]
        {
            get
            {
                if (!this.Properties.ContainsKey(key))
                {
                    this.Properties.Add(key, null);
                }

                return this.Properties[key];
            }
            set
            {
                if (this.Properties.ContainsKey(key))
                {
                    this.Properties[key] = value;
                }
                else
                {
                    this.Properties.Add(key, value);
                }
            }
        }

        /// <summary>
        /// Update the dynamic properties of the current object 
        /// by passing a dictionary of new values
        /// </summary>
        /// <param name="properties">Collection of new values</param>
        public virtual void Update(Dictionary<string, object> properties)
        {
            foreach (var prop in properties)
            {
                this[prop.Key] = prop.Value;
            }
        }
        #endregion

        #region DocumentDb Meta Properties
        [JsonProperty(PropertyName = "_attachments")]
        public dynamic Attachments { get; set; }

        [JsonProperty(PropertyName = "_etag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "_rid")]
        public string Rid { get; set; }

        [JsonProperty(PropertyName = "_self")]
        public string SelfLink { get; set; }

        [JsonProperty(PropertyName = "_ts")]
        public long Timestamp { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "_type")]
        public abstract string DocumentType { get; set; }

        [JsonProperty(PropertyName = "_partitionPath")]
        public abstract string PartitionPath { get; set; }

        public PartitionKey PartitionKey()
        {
            return new PartitionKey(this.PartitionPath);
        }
        #endregion
    }
}