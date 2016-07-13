namespace VsoMigrationTool
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.TeamFoundation.Build.WebApi;
    using Microsoft.TeamFoundation.Core.WebApi;
    using Microsoft.VisualStudio.Services.Client;

    internal class Program
    {
        private static void Main()
        {
            Task.Run(MainAsync).Wait();
        }

        private static async Task MainAsync()
        {
            const string sourceProjectName = "Developer Extensibility";
            const string targetProjectName = "BTS";

            var accountUri = new Uri("https://dynamicscrm.visualstudio.com/defaultcollection");
            var connection = new VssConnection(accountUri, new VssClientCredentials());
            var sourceProject = await FindProjectAsync(connection, sourceProjectName);
            var targetProject = await FindProjectAsync(connection, targetProjectName);

            const string sourceBuildDefinitionName = "Sync2 Continuous Integration";
            const string targetBuildDefinitionName = "Sync Continuous Integration";
            await CopyBuildDefinitionAsync(connection, sourceProject, targetProject, sourceBuildDefinitionName, targetBuildDefinitionName);
        }

        private static async Task<BuildDefinition> CopyBuildDefinitionAsync(
            VssConnection connection,
            TeamProjectReference sourceProject,
            TeamProjectReference targetProject,
            string sourceBuildDefinitionName,
            string targetBuildDefinitionName)
        {
            var buildClient = connection.GetClient<BuildHttpClient>();
            var buildDefinitions = await buildClient.GetDefinitionsAsync(sourceProject.Id);
            foreach (var reference in buildDefinitions)
            {
                if (reference.Name.Equals(sourceBuildDefinitionName, StringComparison.InvariantCultureIgnoreCase))
                {
                    var definition = (BuildDefinition)await buildClient.GetDefinitionAsync(sourceProject.Id, reference.Id);
                    Console.WriteLine($"{definition.Name} ({definition.Id})");

                    definition.Name = targetBuildDefinitionName;
                    definition.Project = targetProject;

                    var result = await buildClient.CreateDefinitionAsync(definition);
                    Console.WriteLine($"Created build '{result.Id}'");

                    return result;
                }
            }

            return null;
        }

        private static async Task<TeamProjectReference> FindProjectAsync(
            VssConnection connection,
            string name)
        {
            var projectClient = connection.GetClient<ProjectHttpClient>();
            var projects = await projectClient.GetProjects();
            foreach (var project in projects)
            {
                if (project.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine($"{project.Name} ({project.Id})");
                    return project;
                }
            }

            return null;
        }
    }
}