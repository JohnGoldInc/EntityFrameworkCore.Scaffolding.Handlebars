// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright(C) 2018 Tony Sneed.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EntityFrameworkCore.Scaffolding.Handlebars;
using EntityFrameworkCore.Scaffolding.Handlebars.Helpers;
using HandlebarsDotNet;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Scaffolding.Handlebars.Tests.Fakes;
using Scaffolding.Handlebars.Tests.Helpers;
using Xunit;
using Constants = EntityFrameworkCore.Scaffolding.Handlebars.Helpers.Constants;

namespace Scaffolding.Handlebars.Tests
{
    public class ReverseEngineeringConfigurationTests
    {
        [Theory]
        [InlineData("Invalid!CSharp*Class&Name")]
        [InlineData("1CSharpClassNameCannotStartWithNumber")]
        [InlineData("volatile")]
        public void ValidateContextNameInReverseEngineerGenerator(string contextName)
        {
            var reverseEngineer = new ServiceCollection()
                .AddEntityFrameworkDesignTimeServices()
                .AddSingleton<ILanguageOptions, LanguageOptions>(x => LanguageOptions.CSharp)
                .AddSingleton<LoggingDefinitions, TestRelationalLoggingDefinitions>()
                .AddSingleton<IRelationalTypeMappingSource, TestRelationalTypeMappingSource>()
                .AddSingleton<IAnnotationCodeGenerator, AnnotationCodeGenerator>()
                .AddSingleton<IDatabaseModelFactory, FakeDatabaseModelFactory>()
                .AddSingleton<IProviderConfigurationCodeGenerator, TestProviderCodeGenerator>()
                .AddSingleton<IScaffoldingModelFactory, FakeScaffoldingModelFactory>()
                .AddSingleton<IModelCodeGenerator, HbsModelGenerator>()
                .AddSingleton<ICSharpDbContextGenerator, HbsDbContextGenerator>()
                .AddSingleton<ICSharpEntityTypeGenerator, HbsEntityTypeGenerator>()
                .AddSingleton<IDbContextTemplateService, HbsDbContextTemplateService>()
                .AddSingleton<ITemplateFileService, InMemoryTemplateFileService>()
                .AddSingleton<ITemplateLanguageService, FakeCSharpTemplateLanguageService>()
                .AddSingleton<IEntityTypeTemplateService, HbsEntityTypeTemplateService>()
                .AddSingleton<IReverseEngineerScaffolder, HbsReverseEngineerScaffolder>()
                .AddSingleton<IEntityTypeTransformationService, HbsEntityTypeTransformationService>()
                .AddSingleton<IContextTransformationService, HbsContextTransformationService>()
                .AddSingleton<IHbsHelperService>(provider => new HbsHelperService(
                    new Dictionary<string, Action<TextWriter, Dictionary<string, object>, object[]>>
                    {
                        {Constants.SpacesHelper, HandlebarsHelpers.SpacesHelper}
                    }))
                .AddSingleton<IHbsBlockHelperService>(provider =>
                    new HbsBlockHelperService(new Dictionary<string, Action<TextWriter, HelperOptions, Dictionary<string, object>, object[]>>()))
                .BuildServiceProvider()
                .GetRequiredService<IReverseEngineerScaffolder>();

            Assert.Equal(DesignStrings.ContextClassNotValidCSharpIdentifier(contextName),
                Assert.Throws<ArgumentException>(
                        () => reverseEngineer.ScaffoldModel(
                            connectionString: "connectionstring",
                            databaseOptions: new DatabaseModelFactoryOptions(),
                            modelOptions: new ModelReverseEngineerOptions(),
                            codeOptions: new ModelCodeGenerationOptions()
                            {
                                ModelNamespace = "FakeNamespace",
                                ContextName = contextName,
                            }))
                    .Message);
        }
    }
}
