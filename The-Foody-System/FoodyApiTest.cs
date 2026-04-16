using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Net;
using The_Foody_System.DTOs;

namespace FoodyExam
{
    [TestFixture]
    public class FoodyApiTests
    {
        private RestClient client;
        private static string foodId;
        private const string BaseUrl = "http://144.91.123.158:81/api/";

        private static string UserName;
        private static string Password;

        [OneTimeSetUp]
        public void Setup()
        {
            UserName = Environment.GetEnvironmentVariable("FOODY_USER");
            Password = Environment.GetEnvironmentVariable("FOODY_PASS");

            Assert.That(UserName, Is.Not.Null.And.Not.Empty, "FOODY_USER is missing.");
            Assert.That(Password, Is.Not.Null.And.Not.Empty, "FOODY_PASS is missing.");

            var unauthenticatedClient = new RestClient(BaseUrl);

            var loginRequest = new RestRequest("User/Authentication", Method.Post);
            loginRequest.AddJsonBody(new UserLoginDTO
            {
                UserName = UserName,
                Password = Password
            });

            var loginResponse = unauthenticatedClient.Execute<LoginResponseDTO>(loginRequest);

            Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(loginResponse.Data, Is.Not.Null);
            Assert.That(loginResponse.Data.AccessToken, Is.Not.Null.And.Not.Empty);

            string token = loginResponse.Data.AccessToken;

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        [Test, Order(1)]
        public void CreateFood_ShouldReturnCreated()
        {
            var request = new RestRequest("Food/Create", Method.Post);

            var food = new FoodDTO
            {
                Name = "TestFood_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                Description = "Test description",
                Url = ""
            };

            request.AddJsonBody(food);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

            var json = System.Text.Json.JsonDocument.Parse(response.Content);
            foodId = json.RootElement.GetProperty("foodId").GetString();

            Assert.That(foodId, Is.Not.Null.And.Not.Empty);
        }

        [Test, Order(2)]
        public void EditFood_Title_ShouldReturnSuccessMessage()
        {
            Assert.That(foodId, Is.Not.Null.And.Not.Empty);

            var request = new RestRequest($"Food/Edit/{foodId}", Method.Patch);

            var patchBody = new[]
            {
                new EditFoodDTO
                {
                    Path = "/name",
                    Op = "replace",
                    Value = "Edited Food Name"
                }
            };

            request.AddJsonBody(patchBody);

            var response = client.Execute<ApiResponseDTO>(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Data, Is.Not.Null);
            Assert.That(response.Data.Msg, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllFoods_ShouldReturnNonEmptyArray()
        {
            var request = new RestRequest("Food/All", Method.Get);

            var response = client.Execute<List<FoodDTO>>(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Data, Is.Not.Null);
            Assert.That(response.Data.Count, Is.GreaterThan(0));
        }

        [Test, Order(4)]
        public void DeleteFood_ShouldReturnSuccessMessage()
        {
            Assert.That(foodId, Is.Not.Null.And.Not.Empty);

            var request = new RestRequest($"Food/Delete/{foodId}", Method.Delete);

            var response = client.Execute<ApiResponseDTO>(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Data, Is.Not.Null);
            Assert.That(response.Data.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateFood_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var request = new RestRequest("Food/Create", Method.Post);

            var invalidFood = new FoodDTO
            {
                Name = string.Empty,
                Description = string.Empty,
                Url = ""
            };

            request.AddJsonBody(invalidFood);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingFood_ShouldReturnNotFound()
        {
            Assert.That(foodId, Is.Not.Null.And.Not.Empty);

            var request = new RestRequest($"Food/Edit/{foodId}", Method.Patch);

            var body = new[]
            {
                new EditFoodDTO
                {
                    Path = "/name",
                    Op = "replace",
                    Value = "InvalidEdit"
                }
            };

            request.AddJsonBody(body);

            var response = client.Execute<ApiResponseDTO>(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Data, Is.Not.Null);
            Assert.That(response.Data.Msg, Is.EqualTo("No food revues..."));
        }

        [Test, Order(7)]
        public void DeleteNonExistingFood_ShouldReturnNotFound()
        {
            var nonExistingFoodId = Guid.NewGuid().ToString("N");

            var request = new RestRequest($"Food/Delete/{nonExistingFoodId}", Method.Delete);

            var response = client.Execute<ApiResponseDTO>(request);

            Assert.That(
                response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest,
                $"Expected NotFound or BadRequest, but was: {response.StatusCode}"
            );

            Assert.That(response.Data, Is.Not.Null);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Assert.That(response.Data.Msg, Is.EqualTo("No food revues..."));
            }
        }
    }
}