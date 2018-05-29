namespace JsonTests.Data
{
    /// <summary>
    /// Very basic example data model.
    /// 
    /// Implements the DynamicDocument base class with a couple 
    /// of extra concrete properties
    /// </summary>
    public class InventoryItem : DynamicDocument
    {
        public string ProductName
        {
            get => this["ProductName"] as string;
            set => this["ProductName"] = value;
        }

        public int StockQuantity
        {
            get => (int)this["StockQuantity"];
            set => this["StockQuantity"] = value;
        }

        public override string DocumentType {
            get { return "InventoryItem"; }
            set { }
        }

        public override string PartitionPath
        {
            get { return this.DocumentType.ToLower(); }
            set { }
        }
    }
}
