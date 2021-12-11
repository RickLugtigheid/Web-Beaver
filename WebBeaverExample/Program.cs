﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using WebBeaver;
using WebBeaver.Framework;

namespace WebBeaverExample
{
	class Program
	{
		static void Main(string[] args)
		{
			// Create a http server
			Http server = new Http(80);
			//Https server = new Https(443);
			/*server.Certificate(
				// Add cert for https
				new X509Certificate2(Http.RootDirectory + "/localhost.pfx", "admin")); */

			// Create a router
			Router router = new Router(server);
			router.Static("public");					// All static file requests will go to the 'public' folder
			router.Log(Http.RootDirectory + "/logs");   // Will log request exceptions to the log folder within the project/dll folder

			router.Engine.Add("tmp", ExampleTemplateEngine);

			// Import routes
			router.Import(Home);                    // Import a route from method
			router.Import(Users);                   // Import a route from method
			router.Import<ApiController>();   // Import all routes from class

			// Adding middleware
			router.middleware += (req, res) =>
			{
				Console.WriteLine("{0} {1}", req.Method, req.Url);
				return true; // Let the router continue handling the request
			};

			// Redirect requests to 500 page if there is an error
			router.onRequestError += (req, res) =>
			{
				res.SendFile("/view/500.html");
			};

			server.Start(); // Start our http server
		}

		#region Routes
		[Route("/")]
		static void Home(Request req, Response res)
		{
			res.SendFile("/view/index.tmp", new { title = "Home", contents = "This is my homepage" });
		}
		[Route("/users")]
		static void Users(Request req, Response res)
		{
			res.SendFile("/view/users.html");
		}
		#endregion

		public static string ExampleTemplateEngine(string fileContents, object args)
		{
			foreach (PropertyInfo prop in args.GetType().GetProperties())
			{
				object value = prop.GetValue(args, null);
				fileContents = fileContents.Replace('%' + prop.Name + '%', (string)value);
			}
			return fileContents;
		}
	}
	[Route("/api")]
	class ApiController
	{
		public static Dictionary<int, string> users = new Dictionary<int, string>()
		{
			{ 0, "Admin" },
			{ 1, "User2" }
		};
		[Route("/user/:id")]
		static void GetUser(Request req, Response res)
		{
			int id = int.Parse(req.Params["id"]);
			if (id >= users.Count)
			{
				res.status = 404;
				res.Send("text/json", "{ \"error\": \"No user found with id '" + id + "'\" }");
			}
			else res.Send("text/json", "{ \"user\": { \"id\": " + id + ", \"name\": \"" + users[id] + "\" } }");
		}
		[Route("POST", "/user")]
		static void AddUser(Request req, Response res)
		{
			users.Add(users.Count, req.Body["name"]);
			Console.WriteLine("Added user: " + req.Body["name"]);
			res.Send("text/json", "{ \"success\": true }");
		}
	}
}
