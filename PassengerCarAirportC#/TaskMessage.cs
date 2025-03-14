using Newtonsoft.Json.Linq;

namespace PassengerTransport
{
    public class TaskMessage
    {
        public string TaskId { get; set; }
        public string TaskType { get; set; }
        public string Point { get; set; }
        public string DetailsString { get; set; }
        public string FlightId { get; set; }
        private JObject _details;
        public JObject Details
        {
            get => _details ?? ParseDetails(DetailsString);
            set => _details = value;
        }

        public JObject ParseDetails(string details)
        {
            try
            {
                return JObject.Parse(details);
            }
            catch
            {
                return new JObject();
            }
        }
    }
}