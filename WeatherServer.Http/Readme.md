# WeatherMcpDemo

To set up ModelInspector in LocalDEV, first set up a self-signed certificate. 
Run the following command with sufficient priviledges to automatically set 
up such a self-signed certificate :

### Setting up self-signed certificate for the MCP server

```ps

# Variables
$certName = "WeatherMcpClientLocalDev2025"
$dnsName = "localhost"
$pfxPath = "C:\temp\$certName.pfx"
$pfxPassword = "YourStrongPassword2025!klmnpQ4xxzz"

Write-Host "Generating self-signed certificate for $dnsName..."

# 1. Create the certificate with SAN and 1-year validity
$cert = New-SelfSignedCertificate `
    -DnsName $dnsName `
    -CertStoreLocation "cert:\LocalMachine\My" `
    -Subject $certName `
    -FriendlyName $certName `
    -NotAfter (Get-Date).AddYears(1)

Write-Host "Certificate created with Thumbprint: $($cert.Thumbprint)"

# 2. Export the certificate to PFX
Write-Host "Exporting certificate to $pfxPath..."
Export-PfxCertificate `
    -Cert "cert:\LocalMachine\My\$($cert.Thumbprint)" `
    -FilePath $pfxPath `
    -Password (ConvertTo-SecureString -String $pfxPassword -Force -AsPlainText)

# 3. Import into Trusted Root Certification Authorities
Write-Host "Importing certificate into Trusted Root Certification Authorities..."

Import-PfxCertificate `
    -FilePath $pfxPath `
    -CertStoreLocation "cert:\LocalMachine\Root" `
    -Password (ConvertTo-SecureString -String $pfxPassword -Force -AsPlainText)

# 3. Import also into the My (Personal) cert store
Import-PfxCertificate `
    -FilePath $pfxPath `
    -CertStoreLocation "cert:\LocalMachine\My" `
    -Password (ConvertTo-SecureString -String $pfxPassword -Force -AsPlainText)


Write-Host "✅ Certificate '$certName' installed and trusted successfully!"
Write-Host "Use this cert in .NET Kestrel with SubjectName: $certName"
```
Afterwards, the certificate should be found by the setup code in the setup `Program.cs`.

ModelInspector expects the SSL traffic to have a valid certificate, but in LocalDEV you can use 
such a self-signed certificate.

Start the server and then start up ModelInspector.

Run this command to start ModelInspector, you will need to have Node installed with a version
that supports modern ES modules and fetch API, so at least Node version 20. I tested with 
Node version 25.

Starting Model Inspector

To start up Model Inspector, start the project WeatherServer.Web.Http and connect
Model Inspector.

Run this command :
```ps
npx @modelcontextprotocol/inspector --startup-url "https://localhost:7145/mcp"
```
Don't worry if you havent installed ModelContextProtocol Inspector, it will be downloaded using npx tool.
You will need Node for this tool installed and Node setup in your PATH environment variable.

In case you still get trouble using SSL in LocalDev due to using self-signed certificates, run this command : 

```ps
$env:NODE_TLS_REJECT_UNAUTHORIZED=0
npx @modelcontextprotocol/inspector --startup-url "https://localhost:7145/mcp"
```
Once inside ModelContextInspector, choose 
- Transport Type set to :
Streamable HTTP
- URL set to: 
https://localhost:7145/sse
- Connection Type set to:
Via Proxy

Once ready, enter the button Connect : Connect
It should say _Connected_ with a green diode indicator.

Screenshot showing the tool in use :
![MCP Inspector running in LocalDev](McpLocalDevPart2.png)

Hit the button _List Tools_ to list the tools in the MCP demo.

You will get the description of each tool and by selecting a tool, you can provide its input 
parameters and also see Description / Instruction usage. 


