namespace TransactionWebService1
{
    public class DataPointDTO
    {
        public string Value { get; set; }

        public DataPointDTO(string value)
        {
            Value = value;
        }

        public DataPointDTO()
        {
            
        }
    }
}