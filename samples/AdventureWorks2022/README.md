# AdventureWorks2022 Sample

## Generate metadata

After successfully building this repo, run the [`GenerateMetadata.cmd`](GenerateMetadata.cmd) script
in this directory. The resulting `.yml` metadata files will be placed in the `metadata` subdirectory.

Note: The script assumes that you have the AdventureWorks2022 database created in a local
SQL Server instance. You might need to adjust the connection string in the script if you
have a different setup.

## Generate documentation

After successfully generating the metadata, you run `docfx.exe` to generate the site based
on the metadata. First install docfx.exe (using `dotnet tool update -g docfx`), if not done so already.

You need to tell DocFx to use the DocDB plugin and templates, which are created during the build of this repo.
They will be located in the `<root>\artifacts\templates` directory, and you need to specify this as an
additional template path when running `docfx.exe build`. The "easiest" soluation is to create a custom
[`docfx.json`](docfx.json) file that includes the relevant directory (in addition to the `default` and `modern` templates.).

Then run DocFx to build (and optionally serve) the site: `docfx.exe build --serv`

For this sample there is also the [`GenerateDocs.cmd`](GeneratedDocs.cmd) script provided,
which you can use.