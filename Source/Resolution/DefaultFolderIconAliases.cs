using System;
using System.Collections.Generic;

namespace MaterialFolderIcons.VisualStudio.Resolution
{
    internal static class DefaultFolderIconAliases
    {
        public static IReadOnlyDictionary<string, string> Build(ISet<string> availableIconKeys)
        {
            var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            Add(aliases, availableIconKeys, new[] { "api", "apis" }, "api");
            Add(aliases, availableIconKeys, new[] { "azure devops", "azure-devops", "azuredevops", "Azure DevOps" }, "azure-devops", "azuredevops");
            Add(aliases, availableIconKeys, new[] { "azure pipelines", "azure-pipelines", "azurepipelines", "Azure Pipelines" }, "azure-pipelines", "azurepipelines");
            Add(aliases, availableIconKeys, new[] { "circleci", "CircleCI", ".circleci" }, "circleci");
            Add(aliases, availableIconKeys, new[] { "github", "GitHub", ".github" }, "github");
            Add(aliases, availableIconKeys, new[] { "gitlab", "GitLab", ".gitlab" }, "gitlab");
            Add(aliases, availableIconKeys, new[] { "git", ".git" }, "git");
            Add(aliases, availableIconKeys, new[] { "node_modules", "node modules", "node-modules" }, "node_modules", "node-modules", "node");
            Add(aliases, availableIconKeys, new[] { "typescript", "TypeScript", "ts" }, "typescript", "ts");
            Add(aliases, availableIconKeys, new[] { "javascript", "JavaScript", "js" }, "javascript", "js");
            Add(aliases, availableIconKeys, new[] { "react", "jsx" }, "react");
            Add(aliases, availableIconKeys, new[] { "vue", "vuejs", "VueJS" }, "vue");
            Add(aliases, availableIconKeys, new[] { "angular" }, "angular");
            Add(aliases, availableIconKeys, new[] { "abstractions", "abstraction" }, "abstractions");
            Add(aliases, availableIconKeys, new[] { "common", "commons", "shared common" }, "common", "shared");
            Add(aliases, availableIconKeys, new[] { "controllers", "controller" }, "controllers", "controller");
            Add(aliases, availableIconKeys, new[] { "entities", "entity" }, "entities", "entity");
            Add(aliases, availableIconKeys, new[] { "extensions", "extension", "ext" }, "extensions", "extension");
            Add(aliases, availableIconKeys, new[] { "interfaces", "interface" }, "interfaces");
            Add(aliases, availableIconKeys, new[] { "services", "service" }, "services", "service");
            Add(aliases, availableIconKeys, new[] { "repositories", "repository", "repo", "repos" }, "repositories", "repository", "repo");
            Add(aliases, availableIconKeys, new[] { "middlewares", "middleware" }, "middlewares", "middleware");
            Add(aliases, availableIconKeys, new[] { "helpers", "helper" }, "helpers", "helper");
            Add(aliases, availableIconKeys, new[] { "models", "model" }, "models", "model");
            Add(aliases, availableIconKeys, new[] { "viewmodels", "view models", "view-models", "viewmodel", "view model", "view-model" }, "viewmodels", "viewmodel");
            Add(aliases, availableIconKeys, new[] { "dtos", "dto", "DTOs", "DTO" }, "dtos", "dto");
            Add(aliases, availableIconKeys, new[] { "enums", "enum", "enumerations", "enumeration" }, "enums");
            Add(aliases, availableIconKeys, new[] { "exceptions", "exception", "errors", "error types" }, "exceptions");
            Add(aliases, availableIconKeys, new[] { "attributes", "attribute", "annotations", "annotation" }, "attributes");
            Add(aliases, availableIconKeys, new[] { "filters", "filter" }, "filters");
            Add(aliases, availableIconKeys, new[] { "validators", "validator", "validation", "validations" }, "validators");
            Add(aliases, availableIconKeys, new[] { "mappers", "mapper", "mappings", "mapping", "profiles", "profile" }, "mappings");
            Add(aliases, availableIconKeys, new[] { "configs", "config", "configuration", "configurations" }, "config", "configs");
            Add(aliases, availableIconKeys, new[] { "options", "option", "settings" }, "options");
            Add(aliases, availableIconKeys, new[] { "data", "datas" }, "data");
            Add(aliases, availableIconKeys, new[] { "migrations", "migration" }, "migrations");
            Add(aliases, availableIconKeys, new[] { "infrastructure", "infra" }, "infrastructure");
            Add(aliases, availableIconKeys, new[] { "application", "application layer" }, "application");
            Add(aliases, availableIconKeys, new[] { "persistence", "persistency" }, "persistence");
            Add(aliases, availableIconKeys, new[] { "areas", "area" }, "areas");
            Add(aliases, availableIconKeys, new[] { "pages", "page", "razor pages", "razor-pages" }, "pages");
            Add(aliases, availableIconKeys, new[] { "commands", "command" }, "commands");
            Add(aliases, availableIconKeys, new[] { "queries", "query" }, "queries");
            Add(aliases, availableIconKeys, new[] { "handlers", "handler", "request handlers", "request-handlers" }, "handlers");
            Add(aliases, availableIconKeys, new[] { "workers", "worker", "hosted services", "hosted-services", "hostedservices", "background services", "background-services", "backgroundservices" }, "workers");
            Add(aliases, availableIconKeys, new[] { "wwwroot", "www root", "www-root", "webroot", "web root", "web-root" }, "wwwroot");
            Add(aliases, availableIconKeys, new[] { "tests", "test", "unit-tests", "unittests", "integration-tests", "integrationtests" }, "tests", "test");
            Add(aliases, availableIconKeys, new[] { "e2e", "end-to-end" }, "e2e");
            Add(aliases, availableIconKeys, new[] { "docs", "documentation" }, "docs");
            Add(aliases, availableIconKeys, new[] { "images", "image", "img" }, "images", "image");
            Add(aliases, availableIconKeys, new[] { "styles", "style", "css", "scss", "sass" }, "styles", "css", "sass");
            Add(aliases, availableIconKeys, new[] { "scripts", "script" }, "scripts", "script");
            Add(aliases, availableIconKeys, new[] { "utils", "util", "utilities" }, "utils");
            Add(aliases, availableIconKeys, new[] { "vendors", "vendor" }, "vendors", "vendor");
            Add(aliases, availableIconKeys, new[] { "packages", "package" }, "packages", "package");
            Add(aliases, availableIconKeys, new[] { "plugins", "plugin" }, "plugins", "plugin");
            Add(aliases, availableIconKeys, new[] { "providers", "provider" }, "providers", "provider");
            Add(aliases, availableIconKeys, new[] { "resources", "resource" }, "resources", "resource");
            Add(aliases, availableIconKeys, new[] { "routes", "route" }, "routes", "route");
            Add(aliases, availableIconKeys, new[] { "screens", "screen" }, "screens", "screen");
            Add(aliases, availableIconKeys, new[] { "layouts", "layout" }, "layouts", "layout");
            Add(aliases, availableIconKeys, new[] { "components", "component" }, "components", "component");
            Add(aliases, availableIconKeys, new[] { "constants", "constant" }, "constants", "constant");
            Add(aliases, availableIconKeys, new[] { "features", "feature" }, "features", "feature");
            Add(aliases, availableIconKeys, new[] { "functions", "function" }, "functions", "function");
            Add(aliases, availableIconKeys, new[] { "google cloud", "gcp", "googlecloud" }, "gcloud");
            Add(aliases, availableIconKeys, new[] { "ios app", "iosapp", "iOS App" }, "iosapp");
            Add(aliases, availableIconKeys, new[] { "macos", "mac os", "osx" }, "mac", "osx");
            Add(aliases, availableIconKeys, new[] { "mongodb", "mongo" }, "mongodb", "mongo");
            Add(aliases, availableIconKeys, new[] { "nuget", "NuGet" }, "nuget");
            Add(aliases, availableIconKeys, new[] { "notifications", "notification" }, "notifications", "notification");
            Add(aliases, availableIconKeys, new[] { "platform.io", "platformio" }, "platformio");
            Add(aliases, availableIconKeys, new[] { "plastic scm", "plastic" }, "plastic");
            Add(aliases, availableIconKeys, new[] { "projects", "project" }, "projects", "project");
            Add(aliases, availableIconKeys, new[] { "resolvers", "resolver" }, "resolvers", "resolver");
            Add(aliases, availableIconKeys, new[] { "seeds", "seed" }, "seeds", "seed");
            Add(aliases, availableIconKeys, new[] { "sources", "source", "src" }, "sources", "source", "src");
            Add(aliases, availableIconKeys, new[] { "visualstudio", "visual studio", "vs" }, "visualstudio", "vs");
            Add(aliases, availableIconKeys, new[] { "web components", "web-components", "webcomponents" }, "webcomponents");

            return aliases;
        }

        private static void Add(IDictionary<string, string> aliases, ISet<string> availableIconKeys, IEnumerable<string> names, params string[] candidateIconKeys)
        {
            var target = FirstExistingIconKey(availableIconKeys, candidateIconKeys);
            if (target == null)
            {
                return;
            }

            foreach (var name in names)
            {
                var normalized = FolderIconResolver.NormalizeFolderName(name);
                if (!string.IsNullOrWhiteSpace(normalized) && !aliases.ContainsKey(normalized))
                {
                    aliases.Add(normalized, target);
                }
            }
        }

        private static string? FirstExistingIconKey(ISet<string> availableIconKeys, IEnumerable<string> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (availableIconKeys.Contains(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
