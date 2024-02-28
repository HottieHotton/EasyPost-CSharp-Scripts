using Newtonsoft.Json;
using EasyPost.Models.API;
using System.Diagnostics;

namespace EasyPost
{

    public class OneCallBuy
    {

        public static async Task Main()
        {
            var dotenv = Path.Combine("../", ".env");
            DotEnv.Load(dotenv);
            string apiKey = Environment.GetEnvironmentVariable("TEST_KEY")!;
            //string apiKey = Environment.GetEnvironmentVariable("PROD_KEY")!;

            Client client = new Client(new ClientConfiguration(apiKey));

            using StreamReader reader = new("../misc.JSON");

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

            dynamic jarray = JsonConvert.DeserializeObject(reader.ReadToEnd());

            string[] properties = ["created_at", "messages", "status", "tracking_code", "updated_at",
                        "batch_id", "batch_status", "batch_message", "id", "order_id",
                        "postage_label", "tracker", "selected_rate", "scan_form", "usps_zone",
                        "refund_status", "mode", "fees", "object", "rates", "insurance", "forms", "verifications"];

#pragma warning disable CS8602 // Dereference of a possibly null reference.

            dynamic[] objects = [jarray["to_address"], jarray["from_address"], jarray["return_address"], jarray["buyer_address"], jarray["parcel"]];

#pragma warning restore CS8602 // Dereference of a possibly null reference.
            CustomsInfo customsInfo = null;

            foreach (dynamic property in properties)
            {
                jarray.Remove(property);
            }

            foreach (dynamic nest in objects)
            {
                foreach (dynamic property in properties)
                {
                    nest.Remove(property);
                }
            }

            if (jarray["customs_info"] == null)
            {
                jarray.Remove("customs_info");
            }
            else if (jarray["customs_info"] != null)
            {
                jarray["customs_info"].Remove("id");
                jarray["customs_info"].Remove("created_at");
                jarray["customs_info"].Remove("updated_at");
                foreach (dynamic item in jarray["customs_info"]["customs_items"])
                {
                    item.Remove("id");
                    item.Remove("mode");
                    item.Remove("created_at");
                    item.Remove("updated_at");
                }
                customsInfo = JsonConvert.DeserializeObject<CustomsInfo>(jarray["customs_info"].ToString());
            }

            if (jarray["options"]["print_custom"] != null)
            {
                jarray["options"].Remove("print_custom");
            }

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            };
            string optionsAsString = JsonConvert.SerializeObject(jarray["options"], jsonSerializerSettings);
            Dictionary<string, object>? dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(optionsAsString);
            string? json = JsonConvert.SerializeObject(dictionary, jsonSerializerSettings);
            Options? options = JsonConvert.DeserializeObject<Options>(json);

            Address toAddress = JsonConvert.DeserializeObject<Address>(jarray["to_address"].ToString());
            Address fromAddress = JsonConvert.DeserializeObject<Address>(jarray["from_address"].ToString());
            Parcel parcel = JsonConvert.DeserializeObject<Parcel>(jarray["parcel"].ToString());


            // Create a shipment using all data in one API call
            Parameters.Shipment.Create parameters = new()
            {
                ToAddress = toAddress,
                FromAddress = fromAddress,
                Parcel = parcel,
                CustomsInfo = customsInfo,
                Options = options,
                Reference = jarray["reference"],
                CarrierAccountIds = ["ca_91540af8b42a4992a2311138c49359d8"],
                Service = "ExpeditedParcel"
            };

            try
            {
                Shipment shipment = await client.Shipment.Create(parameters);
                dynamic shipserialize = JsonConvert.SerializeObject(shipment, Formatting.Indented);
                dynamic ship = JsonConvert.DeserializeObject(shipserialize);
                if (ship["tracking_code"] != null)
                {
                    //Any data you wish to display or open to.
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = ship["postage_label"]["label_url"],
                        UseShellExecute = true
                    });
                }
            }
            catch (Exceptions.API.ApiError error)
            {
                Console.WriteLine(error.PrettyPrint);
            }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        }
    }
}