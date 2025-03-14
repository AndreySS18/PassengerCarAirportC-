using System.Text.Json.Serialization;


namespace PassengerTransport
{
    public class TaskMessage
    {
        public string TaskId { get; set; }
        public string TaskType { get; set; }
        public string Point { get; set; }
        public string DetailsString { get; set; }
        public string FlightId { get; set; }
        public TaskDetails Details { get; set; }
    }
    public class TaskDetails
    {
        [JsonPropertyName("gate")]
        public string Gate { get; set; }
        [JsonPropertyName("takeTo")]
        public string TakeTo { get; set; }
        [JsonPropertyName("passengersCount")]
        public int PassengersCount { get; set; }
    }   
}