using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NET5JsonDemos
{
    /// <summary>
    /// Demo for new .NET 5 JSON features.
    /// </summary>
    class Program
    {
        private static JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            IncludeFields = true,
            WriteIndented = true,
        };

        static async Task Main(string[] args)
        {
            Demo1();
            Demo2();
            await Demo3();
        }

        private static void PrintDemoIntro(int demoNumber)
        {
            Console.WriteLine($"\nStarting demo {demoNumber}\n==============");
        }

        /// <summary>
        /// Shows the following features in action:
        /// - Support preserving object references
        /// - Support (de)serializing quoted numbers
        /// - Support (de)serializing fields
        /// - Support conditionally ignoring properties (always, never, when null/default)
        /// - Copy constructor for JsonSerializerOptions
        /// - Constructor JsonSerializerOptions that takes serialization defaults
        /// </summary>
        private static void Demo1()
        {
            PrintDemoIntro(1);

            var janeEmployee = new Employee
            {
                Name = "Jane Doe",
                YearsEmployed = 10
            };

            var johnEmployee = new Employee
            {
                Name = "John Smith"
            };

            janeEmployee.Reports = new List<Employee> { johnEmployee };
            johnEmployee.Manager = janeEmployee;

            var options = new JsonSerializerOptions(s_serializerOptions)
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
                ReferenceHandler = ReferenceHandler.Preserve,
            };

            string serialized = JsonSerializer.Serialize(janeEmployee, options);
            Console.WriteLine($"Jane serialized: {serialized}");

            Employee janeDeserialized = JsonSerializer.Deserialize<Employee>(serialized, options);
            Console.Write("Whether Jane's first report's manager is Jane: ");
            Console.WriteLine(janeDeserialized!.Reports[0].Manager == janeDeserialized);
        }

        private class Employee
        {
            public string Name { get; internal set; }

            [JsonInclude] // Allows use of non-public property accessor.
            public Employee Manager { get; set; }

            public List<Employee> Reports;

            public int YearsEmployed { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public bool IsManager => Reports?.Count > 0;
        }

        /// <summary>
        /// Shows the following features in action:
        /// - Support deserializing objects using parameterized constructors
        /// - Support non-string dictionary keys
        /// - Opt-in for custom converters to handle null
        /// </summary>
        public static void Demo2()
        {
            PrintDemoIntro(2);

            var point = new Point(1, 2)
            {
                AdditionalValues = new Dictionary<int, int>
                {
                    [3] = 4,
                    [5] = 6,
                }
            };

            string serialized = JsonSerializer.Serialize(point, s_serializerOptions);
            point = JsonSerializer.Deserialize<Point>(serialized, s_serializerOptions);

            Console.WriteLine($"X: {point.X}");
            Console.WriteLine($"Y: {point.Y}");
            Console.WriteLine($"Additional values count: {point.AdditionalValues.Count}");
            Console.WriteLine($"AdditionalValues[3]: {point.AdditionalValues[3]}");
            Console.WriteLine($"AdditionalValues[5]: {point.AdditionalValues[5]}");
            Console.WriteLine($"Description: {point.Description}");
        }

        public class Point
        {
            public int X { get; }

            public int Y { get; }

            public Dictionary<int, int> AdditionalValues;

            [JsonConverter(typeof(DescriptionConverter))]
            public string Description { get; set; }

            [JsonConstructor]
            public Point(int x, int y) => (X, Y) = (x, y);
        }

        public class DescriptionConverter : JsonConverter<string>
        {
            public override bool HandleNull => true;

            [return: MaybeNull]
            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string val = reader.GetString();
                return val ?? "No description provided.";
            }

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value);
            }
        }

        /// <summary>
        /// Shows the JSON extension methods on HttpClient in action.
        /// </summary>
        public static async Task Demo3()
        {
            PrintDemoIntro(3);

            var client = new HttpClient();
            client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");

            // Get the user information.
            User user = await GetUserAsync(client, 1);
            PrintUser(user);

            // Post a new user.
            HttpResponseMessage response = await PostUserAsync(client, user);
            PrintResponseStatus(response);
        }

        public static Task<User> GetUserAsync(HttpClient client, int id)
            => client.GetFromJsonAsync<User>($"users/{id}");

        public static Task<HttpResponseMessage> PostUserAsync(HttpClient client, User user)
            => client.PostAsJsonAsync("users", user);

        static void PrintUser(User customer)
        {
            Console.WriteLine($"Id: {customer.Id}");
            Console.WriteLine($"Name: {customer.Name}");
            Console.WriteLine($"Username: {customer.Username}");
            Console.WriteLine($"Email: {customer.Email}");
        }

        static void PrintResponseStatus(HttpResponseMessage response)
        {
            Console.WriteLine((response.IsSuccessStatusCode ? "Success" : "Error") + $" - {response.StatusCode}");
        }

        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Website { get; set; }
        }
    }
}
