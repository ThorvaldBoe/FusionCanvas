# Design

- Inject the existing `IApplicationVersionProvider` into `SplashWindow` with the production provider as the default.
- Bind a small, high-contrast `Version Major.Minor.Build` label over the splash artwork.
- Cover the binding with an Avalonia headless test using a deterministic provider.
