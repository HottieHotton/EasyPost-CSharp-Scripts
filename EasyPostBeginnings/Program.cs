using Newtonsoft.Json;
using EasyPost.Models.API;
using System.Diagnostics;

namespace EasyPost
{

    public class Examples
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
            }

            if (jarray["options"]["print_custom"] != null)
            {
                jarray["options"].Remove("print_custom");
            }

            // Create a shipment using all data in one API call

            string jarrayAsString = JsonConvert.SerializeObject(jarray, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            });
            Dictionary<string, object>? jarrayAsDictionary = (Dictionary<string, object>?)JsonConvert.DeserializeObject(jarrayAsString, typeof(Dictionary<string, object>), new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            });
#pragma warning disable CS8604 // Possible null reference argument.
            Shipment shipment = await client.Shipment.Create(jarrayAsDictionary);
#pragma warning restore CS8604 // Possible null reference argument.
            dynamic shipserialize = JsonConvert.SerializeObject(shipment, Formatting.Indented);
            dynamic ship = JsonConvert.DeserializeObject(shipserialize);

            try
            {
                if (ship["tracking_code"] == null)
                {
                    if (ship["messages"] != null)
                    {
                        foreach (dynamic message in ship["messages"])
                        {
                            Console.WriteLine(message["carrier"]);
                            Console.WriteLine(message["message"]);
                            Console.WriteLine("-----------------------------------------");
                        }
                    }

                    if (ship["rates"] != null)
                    {
                        foreach (dynamic choice in ship["rates"])
                        {
                            Console.WriteLine(choice["carrier"] + ": " + choice["service"] + " - $" + choice["rate"]);
                            Console.WriteLine(choice["id"]);
                            Console.WriteLine("-----------------------------------------");
                        }
                        Console.WriteLine(ship["id"]);
                    }

                    Console.WriteLine("Do you wish to purchase this shipment? Press enter to continue or type `quit` to stop: ");
                    string user = Console.ReadLine();
                    if (user != "quit")
                    {
                        Console.WriteLine("Please enter the rate you wish to purchase: ");
                        user = Console.ReadLine();

#pragma warning disable CS8604 // Possible null reference argument.

                        Parameters.Shipment.Buy bought = new(user);
                        shipment = await client.Shipment.Buy(shipment.Id, bought);

#pragma warning restore CS8604 // Possible null reference argument.

                        dynamic boughtserialize = JsonConvert.SerializeObject(shipment, Formatting.Indented);
                        Console.WriteLine(boughtserialize);
                        //Any data you wish to display or open to.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = shipment.PostageLabel.LabelUrl,
                            UseShellExecute = true
                        });
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }
                }
                else
                {
                    //Any data you wish to display or open to.
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