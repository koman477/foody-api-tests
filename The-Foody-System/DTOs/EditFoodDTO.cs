using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using System.Text.Json.Serialization;

namespace The_Foody_System.DTOs
{
    public class EditFoodDTO
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("op")]
        public string Op { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
