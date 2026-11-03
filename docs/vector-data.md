# Microsoft.Extensions.VectorData

Microsoft.Extensions.VectorData (MEVD) is a unified API for working with vector
stores in .NET. The abstractions let you perform create, read, update, and
delete operations and run vector similarity search with the same code across
different vector database providers.

## Modeling a record with attributes

You describe the shape of a stored record with attributes on a plain class:

- `[VectorStoreKey]` marks the unique identifier property. The key type can be
  an int, a string, or a Guid.
- `[VectorStoreData]` marks a property you want to store and read back, such as
  the original text or the name of its source file.
- `[VectorStoreVector(dimensions, DistanceFunction = ...)]` marks the embedding
  property, which is a `ReadOnlyMemory<float>`.

The `dimensions` argument must match the number of values your embedding model
produces. If they do not match, the store rejects the vector. The
`DistanceFunction` decides how similarity is measured. With
`DistanceFunction.CosineSimilarity`, a higher score means a closer match.

## Working with a collection

You get a collection (think of it like a table) from a store and then operate on
it:

- `GetCollection<TKey, TRecord>(name)` returns a `VectorStoreCollection`.
- `EnsureCollectionExistsAsync()` creates the collection if it is not there yet.
- `UpsertAsync(record)` inserts or updates one record.
- `SearchAsync(queryVector, top: k)` returns an async stream of
  `VectorSearchResult<TRecord>`, ordered from closest to least close.

Each `VectorSearchResult` carries the original `Record` and a nullable `Score`.
For a larger store you would filter out weak matches by checking the score so
irrelevant records never reach the model.

## In-memory versus real databases

`CommunityToolkit.VectorData.InMemory` provides `InMemoryVectorStore`, which
keeps everything in RAM. It is ideal for learning and tests, but it forgets
everything when the process exits. Because the code is written against the MEVD
abstractions, moving to Qdrant, Azure AI Search, or PostgreSQL with pgvector is
mostly a matter of changing the line that constructs the store. The indexing and
search code stays the same.
