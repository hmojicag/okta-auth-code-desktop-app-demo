# okta-auth-code-desktop-app-demo
Demo .Net app showcasing the way the Authorization Code Flow (OIDC) from Okta might be implemented in a desktop application with a GUI.

This Authentication method is also called sometimes a 3 Legged Authentication method, which uses the Authorization Code grant with PKCE.

This is a **WinForms Desktop Application** written in `C#` using `.Net 8.0`.

All the **Okta OIDC Logic** is inside the `LoginWindow.cs` class which uses a `WebView2` control to display the Okta login page as if it was a web browser.

Follow the next links to know more about the WebView2:
* https://www.nuget.org/packages/Microsoft.Web.WebView2
* https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/winforms

The only 2 external nuget packages used in this project are the WebView2 and Flurl (for the HTTP(s) requests to the OKTA API).

```xml
    <PackageReference Include="Flurl.Http" Version="4.0.1" />
    <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2210.55" />
```

## Creating and Configuring your OKTA App

Remember that your OKTA Applications needs to be an OpenID Connect app, OIDC.

Even though this is a Desktop Application the configuration to be used will be exactly the same as a SPA Single Page Application.

![Create-App.png](img%2FCreate-App.png)

The final configuration of your app will look like:

Remember that this application will only a have a Client Id, no Client Secret should be issued.

Also, Grant Type is Authorization Code.

Redirect URLs could be anything, you can even invent your own domain there, not need for it to exist, we won't use it since we are capturing the redirect event before it even happens.

![Config-App.png](img%2FConfig-App.png)

![Config-App2.png](img%2FConfig-App2.png)

## The Desktop App

Just click the button

![MainForm.png](img%2FMainForm.png)

![Sign-In-WebView2.png](img%2FSign-In-WebView2.png)

![Sign-In-WebView2-Token.png](img%2FSign-In-WebView2-Token.png)