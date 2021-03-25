﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright(C) 2019 Tony Sneed.

using System.IO;
using EntityFrameworkCore.Scaffolding.Handlebars.Internal;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.Extensions.Options;

namespace EntityFrameworkCore.Scaffolding.Handlebars
{
    /// <summary>
    /// Scaffolding generator for DbContext and entity type classes using Handlebars templates.
    /// </summary>
    public class HbsModelGenerator : CSharpModelGenerator
    {
        private readonly IOptions<HandlebarsScaffoldingOptions> _options;
        private readonly ILanguageOptions _languageOptions;

        /// <summary>
        /// Handlebars helper service.
        /// </summary>
        protected virtual IHbsHelperService HandlebarsHelperService { get; }

        /// <summary>
        /// Handlebars helper service.
        /// </summary>
        protected virtual IHbsBlockHelperService HandlebarsBlockHelperService { get; }

        /// <summary>
        /// DbContext template service.
        /// </summary>
        protected virtual IDbContextTemplateService DbContextTemplateService { get; }

        /// <summary>
        /// Entity type template service.
        /// </summary>
        protected virtual IEntityTypeTemplateService EntityTypeTemplateService { get; }

        /// <summary>
        /// Service for transforming entity definitions.
        /// </summary>
        protected virtual IEntityTypeTransformationService EntityTypeTransformationService { get; }

        /// <summary>
        /// Service for transforming context definitions.
        /// </summary>
        protected IContextTransformationService ContextTransformationService { get; }

        /// <summary>
        /// CSharp helper.
        /// </summary>
        public ICSharpHelper CSharpHelper { get; }

        /// <summary>
        /// Constructor for the HbsModelGenerator.
        /// </summary>
        /// <param name="dependencies">Service dependencies parameter class for HbsModelGenerator.</param>
        /// <param name="cSharpDbContextGenerator">DbContext generator.</param>
        /// <param name="cSharpEntityTypeGenerator">Entity type generator.</param>
        /// <param name="handlebarsHelperService">Handlebars helper service.</param>
        /// <param name="handlebarsBlockHelperService">Handlebars block helper service.</param>
        /// <param name="dbContextTemplateService">Template service for DbContext generator.</param>
        /// <param name="entityTypeTemplateService">Template service for the entity types generator.</param>
        /// <param name="entityTypeTransformationService">Service for transforming entity definitions.</param>
        /// <param name="contextTransformationService">Service for transforming context definitions.</param>
        /// <param name="cSharpHelper">CSharp helper.</param>
        /// <param name="options">Handlebar scaffolding options.</param>
        /// <param name="languageOptions">Language Options.</param>
        public HbsModelGenerator(
            [NotNull] ModelCodeGeneratorDependencies dependencies, 
            [NotNull] ICSharpDbContextGenerator cSharpDbContextGenerator, 
            [NotNull] ICSharpEntityTypeGenerator cSharpEntityTypeGenerator,
            [NotNull] IHbsHelperService handlebarsHelperService,
            [NotNull] IHbsBlockHelperService handlebarsBlockHelperService,
            [NotNull] IDbContextTemplateService dbContextTemplateService,
            [NotNull] IEntityTypeTemplateService entityTypeTemplateService,
            [NotNull] IEntityTypeTransformationService entityTypeTransformationService,
            [NotNull] IContextTransformationService contextTransformationService,
            [NotNull] ICSharpHelper cSharpHelper,
            [NotNull] IOptions<HandlebarsScaffoldingOptions> options,
            [NotNull] ILanguageOptions languageOptions)
            : base(dependencies, cSharpDbContextGenerator, cSharpEntityTypeGenerator)
        {
            HandlebarsHelperService = handlebarsHelperService;
            HandlebarsBlockHelperService = handlebarsBlockHelperService;
            DbContextTemplateService = dbContextTemplateService;
            EntityTypeTemplateService = entityTypeTemplateService;
            EntityTypeTransformationService = entityTypeTransformationService;
            ContextTransformationService = contextTransformationService;
            CSharpHelper = cSharpHelper;
            _options = options;
            _languageOptions = languageOptions;
        }

        /// <summary>
        /// <summary>Generates code for a model.</summary>
        /// </summary>
        /// <param name="model"> The model.</param>
        /// <param name="options"> The options to use during generation. </param>
        /// <returns> The generated model. </returns>
        public override ScaffoldedModel GenerateModel(IModel model, ModelCodeGenerationOptions options)
        {
            Check.NotNull(model, nameof(model));
            Check.NotNull(options, nameof(options));

            // Register Hbs helpers and partial templates
            HandlebarsHelperService.RegisterHelpers();
            HandlebarsBlockHelperService.RegisterBlockHelpers();
            DbContextTemplateService.RegisterPartialTemplates();
            EntityTypeTemplateService.RegisterPartialTemplates();

            var resultingFiles = new ScaffoldedModel();

            string generatedCode;

            if (!(CSharpDbContextGenerator is NullCSharpDbContextGenerator))
            {
                generatedCode = CSharpDbContextGenerator.WriteCode(
                    model,
                    options.ContextName,
                    options.ConnectionString,
                    options.ContextNamespace,
                    options.ModelNamespace,
                    options.UseDataAnnotations,
                    options.SuppressConnectionStringWarning,
                    options.SuppressOnConfiguring);

                var dbContextFileName = ContextTransformationService.TransformContextFileName(options.ContextName) + _languageOptions.FileExtension;
                resultingFiles.ContextFile = new ScaffoldedFile
                {
                    Path = options.ContextDir != null
                        ? Path.Combine(options.ContextDir, dbContextFileName)
                        : dbContextFileName,
                    Code = generatedCode
                };
            }

            if (!(CSharpEntityTypeGenerator is NullCSharpEntityTypeGenerator))
            {
                foreach (var entityType in model.GetScaffoldEntityTypes(_options.Value))
                {
                    generatedCode = CSharpEntityTypeGenerator.WriteCode(
                        entityType,
                        options.ModelNamespace,
                        options.UseDataAnnotations);

                    var transformedFileName = EntityTypeTransformationService.TransformEntityFileName(entityType.DisplayName());
                    var entityTypeFileName = _options?.Value?.EnableSchemaFolders == true
                        ? Path.Combine(CSharpHelper.Namespace(entityType.GetSchema()), transformedFileName + _languageOptions.FileExtension)
                        : transformedFileName + _languageOptions.FileExtension;
                    resultingFiles.AdditionalFiles.Add(
                        new ScaffoldedFile
                        {
                            Path = entityTypeFileName, 
                            Code = generatedCode 
                        });
                }
            }

            return resultingFiles;
        }
    }
}
