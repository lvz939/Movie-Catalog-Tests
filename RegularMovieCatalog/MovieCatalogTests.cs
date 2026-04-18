using System;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using RegularMovieCatalog.Models;

namespace RegularMovieCatalog
{

    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string movieId;

        private const string BaseUrl = "http://144.91.123.158:5000";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiI5N2ZjZDgzNS05M2Y0LTRkOGItYjkyYy0wMGZiNDQwY2JmMWYiLCJpYXQiOiIwNC8xOC8yMDI2IDA2OjI0OjEyIiwiVXNlcklkIjoiNzhjZGE4MWItYTdkMS00ZmU0LTYyNGQtMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJpaTgyQGV4YW1wbGUuY29tIiwiVXNlck5hbWUiOiJibGFibGEiLCJleHAiOjE3NzY1MTUwNTIsImlzcyI6Ik1vdmllQ2F0YWxvZ19BcHBfU29mdFVuaSIsImF1ZCI6Ik1vdmllQ2F0YWxvZ19XZWJBUElfU29mdFVuaSJ9.NNTXOj-tSTeyM2YA1ibYuNjoX7ZgobK7W79kD4aw7qg";
        private const string LoginEmail = "ii82@example.com";
        private const string LoginPassword = "string123";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;
            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }
        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
           
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status Code: {response.StatusCode}, Response: {response.Content}");
            }

        }

        [Order(1)]
        [Test]
        public void CreateMovie_WithRequiredFields_ShouldReturnSuccess()
        {

            var movieData = new MovieDTO
            {
                Title = "string123",
                Description = "string123",
                //PosterUrl = "",
                //TrailerLink = "",
                //IsWatched = true

            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
           
            request.AddJsonBody(movieData, ContentType.Json);

            var response = this.client.Execute(request);
           
            var createResponse = JsonSerializer.Deserialize<MovieDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected status code 200 OK ");
           
            var createApiResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(createApiResponse.Msg, Is.EqualTo("Movie created successfully!"));
            Assert.That(createApiResponse, Is.Not.Null);
            movieId = createApiResponse.Movie.Id;
            Assert.That(movieId, Is.Not.Null.And.Not.Empty);
        }

        [Order(2)]
        [Test]

        public void EditMovie_SchouldReturnSuccess()
        {

            var editRequestData = new MovieDTO
            {
                Title = "Edited Idea",
                Description = "This is a edited idea description.",
                //PosterUrl = "",
                //TrailerLink = "",
                //IsWatched = true
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);

            request.AddQueryParameter("movieId", movieId);
            request.AddJsonBody(editRequestData);

            var response = this.client.Execute(request);

            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(editResponse.Msg, Is.EqualTo("Movie edited successfully!"));

        }

        [Order(3)]
        [Test]
        public void GetAllMovies_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);
            var response = this.client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseItems, Is.Not.Empty);
            Assert.That(responseItems, Is.Not.Null);

        }

        [Order(4)]
        [Test]

        public void DeleteMovie_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", movieId);

            var response = this.client.Execute(request);


            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");

            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(deleteResponse.Msg, Is.EqualTo("Movie deleted successfully!"));
        }

        [Order(5)]
        [Test]

        public void CreatedMovie_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var movieData = new MovieDTO
            {
                Title = "",
                Description = "This is a test movie without a title.",
                //PosterUrl = "",
                //TrailerLink = "",
                //IsWatched = true
            };


            RestRequest request = new RestRequest("/api/Movie/Create", Method.Post);

            request.AddJsonBody(movieData);

            RestResponse response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected status code 400 Bad Request");
        }

        [Order(6)]
        [Test]

        public void EditNonExistingMovie_ShouldReturnBadRequest()
        {
            string nonExistingMovieId = "9999999";
            var editRequestData = new MovieDTO

            {
                Title = "Edited MovieExisting",
                Description = "This is a edited movie description.",
                //PosterUrl = "",
                //TrailerLink = "",
                //IsWatched = true
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", nonExistingMovieId);
            request.AddJsonBody(editRequestData);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");

           var readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
           var msg = "Unable to edit the movie! Check the movieId parameter or user verification!";
           Assert.That(readyResponse.Msg, Is.EqualTo(msg));
        }

        [Order(7)]
        [Test]

        public void DeleteNonExistingMovie_ShouldReturnBadRequest()
        {
            string nonExistingMovieId = "9999999";

            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", nonExistingMovieId);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");

            var readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            var msg = "Unable to delete the movie! Check the movieId parameter or user verification!";
            Assert.That(readyResponse.Msg, Is.EqualTo(msg));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }


    }
}
