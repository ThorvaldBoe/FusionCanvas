using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.RejectedPhrases;

namespace FusionCanvas.App.RejectedPhrases;

public sealed record ScopeOption(string Label, RejectedPhraseScope Scope);
