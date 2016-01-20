﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows.Navigation;
using FluentAssertions;
using Microsoft.Common.Core;
using Microsoft.Common.Core.Test.Controls;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Test.Script;
using Microsoft.UnitTests.Core.Threading;
using Microsoft.UnitTests.Core.XUnit;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Help;
using Microsoft.VisualStudio.R.Package.Test.Utility;
using Microsoft.VisualStudio.R.Packages.R;
using Xunit;

namespace Microsoft.VisualStudio.R.Interactive.Test.Help {
    [ExcludeFromCodeCoverage]
    [Collection(CollectionNames.NonParallel)]
    public class HelpWindowTest : InteractiveTest {
        [Test]
        [Category.Interactive]
        public void HelpTest() {
            var clientApp = new RHostClientHelpTestApp();
            using (var hostScript = new VsRHostScript(clientApp)) {
                using (var script = new ControlTestScript(typeof(HelpWindowVisualComponent))) {
                    DoIdle(100);

                    var component = ControlWindow.Component as IHelpWindowVisualComponent;
                    component.Should().NotBeNull();

                    clientApp.Component = component;

                    ShowHelp("?plot\n", hostScript, clientApp);
                    clientApp.Uri.IsLoopback.Should().Be(true);
                    clientApp.Uri.PathAndQuery.Should().Be("/library/graphics/html/plot.html");

                    ShowHelp("?lm\n", hostScript, clientApp);
                    clientApp.Uri.PathAndQuery.Should().Be("/library/stats/html/lm.html");

                    ExecCommand(clientApp, RPackageCommandId.icmdHelpPrevious);
                    clientApp.Uri.PathAndQuery.Should().Be("/library/graphics/html/plot.html");

                    ExecCommand(clientApp, RPackageCommandId.icmdHelpNext);
                    clientApp.Uri.PathAndQuery.Should().Be("/library/stats/html/lm.html");

                    ExecCommand(clientApp, RPackageCommandId.icmdHelpHome);
                    clientApp.Uri.PathAndQuery.Should().Be("/doc/html/index.html");
                }
            }
        }

        private void ShowHelp(string command, VsRHostScript hostScript, RHostClientHelpTestApp clientApp) {
            clientApp.Ready = false;
            using (var request = hostScript.Session.BeginInteractionAsync().Result) {
                request.RespondAsync(command).SilenceException<RException>();
            }
            WaitForAppReady(clientApp);
        }

        private void ExecCommand(RHostClientHelpTestApp clientApp, int commandId) {
            UIThreadHelper.Instance.Invoke(() => {
                clientApp.Ready = false;
                object o = new object();
                clientApp.Component.Controller.Invoke(RGuidList.RCmdSetGuid, commandId, null, ref o);
            });
            WaitForAppReady(clientApp);
        }

        private void WaitForAppReady(RHostClientHelpTestApp clientApp) {
            for (int i = 0; i < 100 && !clientApp.Ready; i++) {
                DoIdle(200);
            }
        }

        class RHostClientHelpTestApp : RHostClientTestApp {
            IHelpWindowVisualComponent _component;
            public IHelpWindowVisualComponent Component {
                get { return _component; }
                set {
                    _component = value;
                    _component.Browser.Navigated += Browser_Navigated;
                }
            }
            public bool Ready { get; set; }
            public Uri Uri { get; private set; }
            private void Browser_Navigated(object sender, NavigationEventArgs e) {
                Ready = true;
                Uri = _component.Browser.Source;
            }

            public override Task ShowHelp(string url) {
                UIThreadHelper.Instance.Invoke(() => {
                    Component.Navigate(url);
                });
                return Task.CompletedTask;
            }
        }
    }
}
