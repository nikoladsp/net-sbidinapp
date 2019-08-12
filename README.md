# net-sbidinapp
### Functional examples of inapp authentication

---

This sample illustrates the API calls required to make the flow work as expected. It can be viewed as a jumping-off point for your app implementation.

**Dependencies**:

* .NET Framework 4.7.2
* [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/): JSON library

### Supported eIDs.

* [Program.cs](./Program.cs): Swedish BankID.

### Flow

1. Call /authorize.
2. Poll collect method until success.
3. Call complete method - the last redirect will contain CODE and STATE.
4. Call /token end-point as normal (using CODE we got in STEP 3).
5. Call /userinfo with access token. (optional)

### Application Usage

**Swedish BankID** ([Program.cs](./Program.cs))

You need to change the variable ```string nid = "197602208253";``` to a valid Swedish BankID test-user.