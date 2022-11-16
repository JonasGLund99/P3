using Microsoft.Azure.Cosmos;

namespace src.Data;

public static class ExperimentManager
{
    public static async Task<List<Experiment>> QueryExperiments(string searchParameter)
    {
        List<Experiment> experiments = new List<Experiment>();
        if (DatabaseService.Instance.Database == null)
        {
            throw new NullReferenceException("No database");
        }
        
        string queryString = @"SELECT * FROM Experiment 
                            WHERE CONTAINS(Experiment.ExperimentNumber, @searchParameter, true)
                            OR CONTAINS(Experiment.Title, @searchParameter, true)
                            OR CONTAINS(Experiment.Author, @searchParameter, true)";

        FeedIterator<Experiment> feed = DatabaseService.Instance.Database.GetContainer("Experiment")
                                        .GetItemQueryIterator<Experiment>(
                                            queryDefinition: new QueryDefinition(queryString)
                                            .WithParameter("@searchParameter", searchParameter)
                                        );

        while (feed.HasMoreResults)
        {
            FeedResponse<Experiment> response = await feed.ReadNextAsync();
            experiments.AddRange(response);
        }
        return experiments;
    }

    // Deletes the relation between an experiment and clinical test, 
    // and deletes the clinical test if it is not related to any other experiments
    public static async Task Disassociate(Experiment experiment, ClinicalTest clinicalTest)
    {
        experiment.ClinicalTestIds.Remove(clinicalTest.id);
        await experiment.SaveToDatabase();

        clinicalTest.ExperimentIds.Remove(experiment.id);
        
        if (clinicalTest.ExperimentIds.Count == 0)
        {
            await clinicalTest.RemoveFromDatabase();
        } 
        else 
        {
            await clinicalTest.SaveToDatabase();
        }
    }
    
   public static async Task Associate(Experiment experiment, ClinicalTest clinicalTest)
   {
      if ( !experiment.ClinicalTestIds.Contains(clinicalTest.id))
      {
         experiment.ClinicalTestIds.Add(clinicalTest.id);
         await experiment.SaveToDatabase();

         clinicalTest.ExperimentIds.Add(experiment.id);
         await clinicalTest.SaveToDatabase();
      }
   }

    public static async Task DeleteExperiment(Experiment experiment)
    {
        List<ClinicalTest> clinicalTests = await experiment.QueryClinicalTests(""); 
        foreach (ClinicalTest clinicalTest in clinicalTests)
        {
            await Disassociate(experiment, clinicalTest);
        }
        await experiment.RemoveFromDatabase();
    }

   public static async Task DeleteClinicalTest(ClinicalTest clinicalTest)
   {
      //Vi skal have lavet en funktion som kan query experiment eller clinical test baseret på deres id.
      //Så vi kan få et håndtag direkte til et experiment eller clinicalTest fra databasen.
      List<Experiment> experiments = await QueryExperiments("");
      foreach (Experiment e in experiments)
      {
         if (e.ClinicalTestIds.Contains(clinicalTest.id))
         {
            await Disassociate(e, clinicalTest);
         }
      }
   }

}
