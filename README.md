# fabric-open-mirroring-sample
A complete sample about Microsoft Fabric Open mirroring feature

## What is Open Mirroring


## Run this sample

To run this sample, you'll need: 
- .NET 9 SDK installed (the repo comes with a devcontainer configuration)
- A Fabric Tenant, with a workspace attached to a Fabric capacity (F, P, or trial)

## Authenticate to your Fabric tenant

There are multiple ways to authenticate to your Fabric tenant. 

### Bearer token

> **Warning** This is for demo purposes only! Token is valid for about 1 hour.

Execute the following code in a notebook cell from a user who has access to the mirrored database. You'll need to pass on the base64 encoded token to the `setup-mirroring` command., without the "`b'`..." prefix and "...`'`" suffix.

```python
from notebookutils import mssparkutils
import base64
token = mssparkutils.credentials.getToken('https://storage.azure.com/.default')
encoded = base64.b64encode(token.encode())
print(encoded)
```
