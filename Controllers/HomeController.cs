using GroqSharp;
using BetterHumans.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using BetterHumans.Models;

namespace AIintegration.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        // Passing this in using dependency injection:
        private IGroqClient _groqClient;

        public HomeController(ILogger<HomeController> logger, IGroqClient groqClient) // the Services container passes in the IGrogClient object
        {
            _logger = logger;
            _groqClient = groqClient; // initializes instance field right here
        }

        // This is the starting point of the application!! It simply sends out the view Index.cshtml to client.
        [HttpGet] // I added this. It is implied but I added it just so things make more sense to the reader.
        public IActionResult Index()
        {
            return View();
        }



        [HttpPost, ActionName("UploadFile")]
        public IActionResult UploadFile(IFormFile uploadedFile)
        {
            string name = null; // Initialize name

            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                // Ensure the uploaded file is a text file 
                if (Path.GetExtension(uploadedFile.FileName).ToLower() == ".txt")
                {
                    using (var reader = new StreamReader(uploadedFile.OpenReadStream()))
                    {
                        // Read the content of the file
                        name = reader.ReadToEnd();
                    }

                    // Store the file content in TempData to be used in AnalyzeFile
                    TempData["FileText"] = name;

                    // Also pass it to ViewBag for immediate display
                    ViewBag.FileText = $"File uploaded successfully. The content of the file is: {name}";
                }
                else
                {
                    ViewBag.FileText = "Please upload a valid text file (.txt).";
                }
            }
            else
            {
                ViewBag.FileText = "No file was uploaded.";
            }

            return View("Index");
        }

        [HttpPost, ActionName("AnalyzeFile")]
        public async Task<IActionResult> AnalyzeFile(string question)
        {
            // Generate the comprehension question
            string comprehensionQuestion = $"Please come up with a comprehension question in 100 characters or less for the following passage: {question}";
            string generatedQuestion = await _groqClient.CreateChatCompletionAsync(new GroqSharp.Models.Message { Content = comprehensionQuestion });

            // Generate the answer to the comprehension question
            string comprehensionAnswerPrompt = $"Please provide an answer to the following comprehension question: {generatedQuestion} that is being asked about the following passage: {question}";
            string generatedAnswer = await _groqClient.CreateChatCompletionAsync(new GroqSharp.Models.Message { Content = comprehensionAnswerPrompt });

            // Store the generated question and answer in TempData
            TempData["GeneratedQuestion"] = generatedQuestion;
            TempData["GeneratedAnswer"] = generatedAnswer;
            TempData.Keep("FileText");  // Keep the file text from previous request

            // Store the generated question and answer in ViewBag for immediate display
            ViewBag.GeneratedQuestion = generatedQuestion;
            ViewBag.GeneratedAnswer = generatedAnswer;
            ViewBag.FileText = TempData["FileText"];

            return View("Index");
        }


        // Action to handle the user's submitted answer
        [HttpPost, ActionName("SubmitUserAnswer")]
        public async Task<IActionResult> SubmitUserAnswer(string userAnswer)
        {
            // Retrieve previously generated question and answer from TempData
            string generatedQuestion = TempData.Peek("GeneratedQuestion")?.ToString();
            string generatedAnswer = TempData.Peek("GeneratedAnswer")?.ToString();
            string fileText = TempData.Peek("FileText")?.ToString();

            // Store them in ViewBag
            ViewBag.GeneratedQuestion = generatedQuestion;
            ViewBag.GeneratedAnswer = generatedAnswer;
            ViewBag.FileText = fileText;
            ViewBag.UserAnswer = userAnswer;

            // Use Groq to compare the user's answer and the generated answer
            string comparisonPrompt = $"Please compare the user's answer: '{userAnswer}' with the correct answer: '{generatedAnswer}' " +
                                      "and provide an assessment of how well the user understood the text.";

            // Send the prompt to Groq and get the assessment response
            string comprehensionAssessment = await _groqClient.CreateChatCompletionAsync(new GroqSharp.Models.Message { Content = comparisonPrompt });

            // Store the assessment in ViewBag
            ViewBag.Assessment = comprehensionAssessment;

            // Keep TempData values for future requests
            TempData.Keep("GeneratedQuestion");
            TempData.Keep("GeneratedAnswer");
            TempData.Keep("FileText");

            return View("Index");
        }





        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}