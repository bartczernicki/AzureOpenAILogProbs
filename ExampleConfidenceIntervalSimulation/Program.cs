namespace ExampleConfidenceIntervalSimulation
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const int NUMBEROFSIMULATIONS = 10000000; // 10,000,000 simulations, make this smaller for faster results

            // This is what the AI model claims the confidence is for the various questions
            const int CLAIMEDCONFIDENCEOFAIMODEL = 80; // 80% confidence

            Console.WriteLine($"Simulating {NUMBEROFSIMULATIONS:n0} iterations with a claimed AI LLM Model confidence of {CLAIMEDCONFIDENCEOFAIMODEL}% for 100 questions");

            var random = new Random();  // Add seed for reproducibility
            var bootstrapConfidenceScores = new List<double>();
            for (int i = 0; i != NUMBEROFSIMULATIONS; i++) // Bootstrap Simulations (bootstrap estimates)
            {
                var bootstrapSample = new List<double>();
                for (int j = 0; j != 100; j++)
                {
                    var randomIndex = random.Next(0, 100);

                    if (randomIndex < CLAIMEDCONFIDENCEOFAIMODEL)
                    {
                        bootstrapSample.Add(1);
                    }
                }

                bootstrapConfidenceScores.Add(bootstrapSample.Count());
            }

            // Sort the confidence scores to calculate the percentiles
            var bootstrapConfidenceScoresSorted = bootstrapConfidenceScores.OrderBy(a => a).ToList();
            // Calculate the min and the max
            var minimumScore = Math.Round(bootstrapConfidenceScoresSorted.Min(), 3);
            var maximumScore = Math.Round(bootstrapConfidenceScoresSorted.Max(), 3);
            // Calculate the 2.5% and 97.5% percentiles
            var lowerPercentile = Math.Round(bootstrapConfidenceScoresSorted[250000], 3);
            var upperPercentile = Math.Round(bootstrapConfidenceScoresSorted[9750000], 3);

            // Cout how many times the score was less than 50
            var lessThan50 = bootstrapConfidenceScoresSorted.Count(a => a < 50);
            // Count how many times the score was greater than 90
            var greaterThan90 = bootstrapConfidenceScoresSorted.Count(a => a > 90);

            Console.WriteLine($"Minimum Amount of Questions Answered (assuming {CLAIMEDCONFIDENCEOFAIMODEL}% claimed confidence) in {NUMBEROFSIMULATIONS:n0} simulations: {bootstrapConfidenceScoresSorted.First()}");
            Console.WriteLine($"Maximum Amount of Questions Answered (assuming {CLAIMEDCONFIDENCEOFAIMODEL}% claimed confidence) in {NUMBEROFSIMULATIONS:n0} simulations: {bootstrapConfidenceScoresSorted.Last()}");

            // These percentiles will converge to the true confidence interval as the number of simulations increases
            Console.WriteLine($"Lower 2.5% question bound: {lowerPercentile}");
            Console.WriteLine($"Upper 2.5% question bound: {upperPercentile}");

            Console.WriteLine($"%    Less than 50 Questions Answered Correctly: {(1.0 * lessThan50) / NUMBEROFSIMULATIONS}");
            Console.WriteLine($"% Greater than 90 Questions Answered Correctly: {(1.0 * greaterThan90) / NUMBEROFSIMULATIONS}");
        }
    }
}
