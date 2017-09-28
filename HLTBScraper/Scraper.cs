using System;
using System.IO;
using System.Net;
using System.Text;
using HtmlAgilityPack;

namespace HLTBScraper
{
    class Scraper
    {
        private static bool displayLogs = true; // Determines if game names or HTTP request statuses should be display on console.
        private static string CSVPath = "C:\\Users\\YourUsername\\Desktop\\GameEntries.csv"; // Path the final data will be saved to
        private static string currLetter = "A"; // Current character that will be searched for in game database.
        private static int gameID = 1; // ID of current scraped entry

        static void Main(string[] args)
        {
            // Get all games numbered 0 - 9
            for (int i = 0; i <= 9; i++)
            {
                currLetter = "" + i;
                string games = GetHLTBGames(currLetter, 1);
                ParseHTML(games);
            }

            // Get all games that start with #, $, /, ., :, [
            string specialCharGames = "#$/.:[";
            foreach (char c in specialCharGames)
            {
                currLetter = "" + c;
                string games = GetHLTBGames(currLetter, 1);
                ParseHTML(games);
            }

            // Get all games in alphabet
            for (char c = 'A'; c <= 'Z'; c++)
            {
                currLetter = "" + c;
                string games = GetHLTBGames(currLetter, 1);
                ParseHTML(games);
            }
        }

        /// <summary>
        /// Parses the input HTML string and tries to build a Game object from the data.
        /// </summary>
        /// <param name="html">String containing the HTML to be parsed.</param>
        private static void ParseHTML(string html)
        {
            // Load HTML File
            HtmlDocument doc = new HtmlDocument();
            try
            {
                doc.LoadHtml(html);
            }
            catch (Exception e)
            {
                doc = null;
                Console.WriteLine(e);
            }

            if (doc == null)
            {
                Console.WriteLine("Document could not be opened or found!");
                return;
            }

            int numPages = 1;

            HtmlNodeCollection page = doc.DocumentNode.SelectNodes("//*[starts-with(@class,'search_list_page back_darkish shadow_box')]");

            if (page != null)
            {
                numPages = int.Parse(page[page.Count - 1].InnerText.Trim());                
            }

            Game entry = new Game();

            // Loop through every page and get game entry info
            for (int i = 0; i < numPages; i++)
            {
                HtmlNodeCollection gameEntries = doc.DocumentNode.SelectNodes("//*[starts-with(@class,'back_white shadow_box')]");

                if (gameEntries == null)
                {
                    return;
                }

                // Loop through every game entry
                for (int x = 0; x < gameEntries.Count; x++)
                {
                    // Create new game object to store scraped data
                    Game newGameEntry = new Game();
                    newGameEntry.gameID = gameID;

                    // Get game image
                    HtmlNodeCollection gameImage = gameEntries[x].SelectNodes("//*[starts-with(@src,'gameimages')]");

                    string imgUrl = gameImage[x].Attributes[0].Value;
                    string gameDetailsUrl = gameImage[x].ParentNode.Attributes[1].Value;

                    imgUrl = imgUrl.Replace(",", "^^^^"); // Replace commas with ^^^^ to prevent comma issues in csv

                    newGameEntry.boxArt = imgUrl;
                    newGameEntry.gameDetailsUrl = gameDetailsUrl;

                    // Get completion details
                    HtmlNodeCollection gameTitles = gameEntries[x].SelectNodes("//*[starts-with(@class,'text_')]");

                    string gameTitle = gameTitles[x].ChildNodes[0].InnerText;

                    gameTitle = gameTitle.Replace(",", "^^^^");

                    newGameEntry.gameName = gameTitle;

                    HtmlNodeCollection gameDetails = gameEntries[x].SelectNodes("//*[starts-with(@class,'search_list_details_block')]");

                    string mainStoryLength = "N/A";
                    string mainExtraLength = "N/A";
                    string completionistLength = "N/A";
                    string combinedLength = "N/A";

                    char[] separatingChars = { ' ', '\n' };

                    // Make sure game has time entries filled in
                    if (gameDetails[x].ChildNodes.Count >= 2)
                    {
                        // Main story length
                        mainStoryLength = gameDetails[x].ChildNodes[1].InnerText;

                        string[] results = mainStoryLength.Split(separatingChars, System.StringSplitOptions.RemoveEmptyEntries);

                        // If game is a normal entry
                        if (results.Length >= 8) // Each length creates 2 array entries if it is empty and 4 if it was assigned a value. The minimum amount of entries is 8 and the maximum is 16
                        {
                            // Every game length was filled out
                            if (results.Length == 16)
                            {
                                mainStoryLength = results[2].Replace("&#189;", ".5");
                                mainExtraLength = results[7].Replace("&#189;", ".5");
                                completionistLength = results[10].Replace("&#189;", ".5");
                                combinedLength = results[14].Replace("&#189;", ".5");
                            }
                            else // Game lengths are missing so find the first valid game length and use it for every length band
                            {
                                // Loop through the split string and populate the game lengths
                                for (int q = 0; q < results.Length; q++)
                                {
                                    string length = results[q].Replace("&#189;", ".5");

                                    if (float.TryParse(length, out var time)) {
                                        mainStoryLength = length;
                                        mainExtraLength = length;
                                        completionistLength = length;
                                        combinedLength = length;
                                        break;
                                    }
                                    else
                                    {
                                        mainStoryLength = "N/A";
                                        mainExtraLength = "N/A";
                                        completionistLength = "N/A";
                                        combinedLength = "N/A";
                                    }
                                }
                            }
                        }
                        else // If game does not follow standard entry pattern (Game is listed as 'Co-Op, Vs., or Solo and does not have every length filled out)
                        {
                            mainStoryLength = gameDetails[x].ChildNodes[3].InnerText;

                            if (!mainStoryLength.Contains("--"))
                            {
                                results = mainStoryLength.Split(separatingChars, System.StringSplitOptions.RemoveEmptyEntries);

                                // If not every game length exists, set all to the time that was provided
                                mainStoryLength = results[0].Replace("&#189;", ".5");
                                mainExtraLength = results[0].Replace("&#189;", ".5");
                                completionistLength = results[0].Replace("&#189;", ".5");
                                combinedLength = results[0].Replace("&#189;", ".5");
                            }
                            else
                            {
                                mainStoryLength = "N/A";
                            }
                        }
                    }

                    newGameEntry.mainStoryLength = mainStoryLength;
                    newGameEntry.mainExtraLength = mainExtraLength;
                    newGameEntry.completionistLength = completionistLength;
                    newGameEntry.combinedLength = combinedLength;

                    // Print game title
                    if (displayLogs)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("---" + gameTitle + "---");
                        Console.ForegroundColor = ConsoleColor.White;

                        // Print game lengths
                        //Console.WriteLine(mainStoryLength);
                        //Console.WriteLine(mainExtraLength);
                        //Console.WriteLine(completionistLength);
                        //Console.WriteLine(combinedLength);
                        //Console.WriteLine("-------------------------------");

                        //Console.WriteLine(gameImage[x].Attributes[0].Value); // Print img url
                        //Console.WriteLine(gameImage[x].ParentNode.Attributes[1].Value); // Print details url
                        //Console.WriteLine(gameTitles[x].ChildNodes[0].InnerText); // Print game title
                    }

                    // Write game to file
                    WriteEntry(newGameEntry);

                    // Increment ID for next game
                    gameID++;
                }

                // Connect to next page
                html = GetHLTBGames(currLetter, (i+2));
                try
                {
                    doc.LoadHtml(html);
                }
                catch (Exception e)
                {
                    doc = null;
                    Console.WriteLine(e);
                }
            }
        }

        private static string GetHLTBGames(string gameName, int pageNum)
        {
            // Create POST request
            WebRequest request = WebRequest.Create("https://howlongtobeat.com/search_main.php?page=" + pageNum);
            request.Method = "POST";

            // Form POST body
            string queryString = "queryString=" + gameName;
            string postData = queryString + "&t=games&sorthead=popular&sortd=Normal Order&plat=&length_type=main&length_min=&length_max=&detail=0";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            // Set ContentType
            request.ContentType = "application/x-www-form-urlencoded";

            // Set ContentLength
            request.ContentLength = byteArray.Length;

            // Get the request stream
            Stream dataStream = request.GetRequestStream();

            // Write the data to the request stream
            dataStream.Write(byteArray, 0, byteArray.Length);

            // Close the Stream object 
            dataStream.Close();

            // Get the response 
            WebResponse response = request.GetResponse();

            if (displayLogs)
            {
                // Display the status  
                Console.WriteLine("----------------------------------------------------------");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("WEB RESPONSE STATUS: " + ((HttpWebResponse)response).StatusDescription);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("----------------------------------------------------------");
                Console.WriteLine();
            }

            // Get the stream containing content from server
            dataStream = response.GetResponseStream();

            // Read the content 
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();

            // Clean up  
            reader.Close();
            dataStream.Close();
            response.Close();

            return responseFromServer;
        }

        /// <summary>
        /// Writes a Game object to a csv file
        /// </summary>
        /// <param name="game">Game entry to be added to the file.</param>
        private static void WriteEntry(Game game)
        {
            StringBuilder gameData = new StringBuilder();

            string coreEntry = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", gameID, game.gameName, game.boxArt, game.gameDetailsUrl, game.mainStoryLength, game.mainExtraLength, game.completionistLength, game.combinedLength);
            gameData.AppendLine(coreEntry);

            // If csv doesn't exist, create a new one with the proper heading
            if (!File.Exists(CSVPath))
            {
                string heading = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", "GAME_ID", "NAME", "BOX_ART", "DETAILS_URL", "MAIN_STORY_LENGTH", "MAIN_EXTRA_LENGTH", "COMPLETIONIST_LENGTH", "COMBINED_LENGTH");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(heading);

                File.AppendAllText(CSVPath, sb.ToString());
            }

            File.AppendAllText(CSVPath, gameData.ToString());
        }
    }
}