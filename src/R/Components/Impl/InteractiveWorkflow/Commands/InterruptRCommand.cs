﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Common.Core.UI.Commands;
using Microsoft.R.Host.Client;

namespace Microsoft.R.Components.InteractiveWorkflow.Commands {
    public sealed class InterruptRCommand : IAsyncCommand {
        private readonly IRInteractiveWorkflow _interactiveWorkflow;
        private readonly IRSession _session;
        private readonly IDebuggerModeTracker _debuggerModeTracker;

        public InterruptRCommand(IRInteractiveWorkflow interactiveWorkflow, IDebuggerModeTracker debuggerModeTracker) {
            _interactiveWorkflow = interactiveWorkflow;
            _session = interactiveWorkflow.RSession;
            _debuggerModeTracker = debuggerModeTracker;
        }

        public CommandStatus Status {
            get {
                var status = CommandStatus.Supported;
                if (_interactiveWorkflow.ActiveWindow == null) {
                    status |= CommandStatus.Invisible;
                } else if (CanInterrupt()) {
                    status |= CommandStatus.Enabled;
                }
                return status;
            }
        }

        public async Task InvokeAsync() {
            if (CanInterrupt()) {
                _interactiveWorkflow.Operations.ClearPendingInputs();
                await _session.CancelAllAsync();
            }
        }

        private bool CanInterrupt()
            => _session.IsHostRunning && _session.IsProcessing && !_session.IsReadingUserInput && !_debuggerModeTracker.IsInBreakMode;
    }
}
