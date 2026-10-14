# Library

Git-backed Library institution for the Aetheric Forge web shell.

The runtime contracts and provider implementations remain reusable dependencies, pinned as submodules. The shell loads this module independently and never references its implementation project.

```sh
git submodule update --init --recursive
dotnet test tests/Library.Web.Tests --configuration Release
dotnet publish Library.Web --configuration Release
```

The plugin uses shared runtime contracts and private infrastructure dependencies. It retains its collectible assembly context while hosted. Definitions and bindings are under `institution/`; bindings contain deployment examples, never credentials.

Credentials are institution-owned under `Library:MongoDb`.

Article routes remain `/library` and `/library/articles/{id}` with authentication required. Existing articles retain the `ForgeCampus` knowledge scheme, `knowledge` collection, `forge-library` authority, canonical IDs, and revisions. Additional owner-approved knowledge providers can be contributed under the `Library` DI key. The module does not replace another module's services.
