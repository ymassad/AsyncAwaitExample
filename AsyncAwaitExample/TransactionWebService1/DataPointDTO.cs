namespace TransactionWebService
{
    public class DataPointDTO
    {
        public int DataPointId { get; set; }

        public string Value { get; set; }

        public DataPointDTO(int dataPointId, string value)
        {
            DataPointId = dataPointId;
            Value = value;
        }

        public DataPointDTO()
        {
            
        }
    }
}