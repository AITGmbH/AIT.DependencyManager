using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using AIT.DMF.DependencyManager.Controls.Services;
using Moq;

namespace AIT.DMF.DependencyManager.Controls.Test.ViewModels
{
    public class TestBase
    {
        protected static void _InitializeDependenyInjectionService<T>(Action<T> setup) where T : class
        {
            // create an ICompositonService stub and stub the instance methods using Moq
            var compositorFake = new Mock<ICompositionService>();
            compositorFake.Setup(o => o.SatisfyImportsOnce(It.IsAny<ComposablePart>()))
                .Callback<object>(o =>
                {
                    var viewModel = o as T;
                    if (viewModel != null)
                    {
                        if (setup != null)
                        {
                            setup(viewModel);
                        }

                        var partImportsSatisfiedNotification = o as IPartImportsSatisfiedNotification;
                        if (partImportsSatisfiedNotification != null)
                        {
                            partImportsSatisfiedNotification.OnImportsSatisfied();
                        }
                    }
                });

            // stub the required extension method using moles
            System.ComponentModel.Composition.Moles.MAttributedModelServices.SatisfyImportsOnceICompositionServiceObject = (compositionService, o) =>
            {
                var viewModel = o as T;
                if (viewModel != null)
                {
                    if (setup != null)
                    {
                        setup(viewModel);
                    }

                    var partImportsSatisfiedNotification = o as IPartImportsSatisfiedNotification;
                    if (partImportsSatisfiedNotification != null)
                    {
                        partImportsSatisfiedNotification.OnImportsSatisfied();
                    }
                }

                return null;
            };

            // stub the dependency injection service
            Controls.Services.Moles.MDependencyInjectionService.AllInstances.CompositionServiceGet = dependencyInjectionService => compositorFake.Object;
            Controls.Services.Moles.MDependencyInjectionService.AllInstances.GetDependency<T>(o =>
                                                                                                  {
                                                                                                      throw new NotImplementedException();
                                                                                                  });
            Controls.Services.Moles.MDependencyInjectionService.AllInstances.GetDependencyString<T>((o, contractName) =>
                                                                                                {
                                                                                                    throw new NotImplementedException();
                                                                                                });
        }
    }
}
