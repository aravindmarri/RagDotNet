# The RAG pipeline

Retrieval-augmented generation grounds a language model in your own data. The
model answers from passages you retrieve at query time instead of from what it
memorized during training. This removes most made-up answers and lets you update
knowledge by changing documents rather than retraining a model.

## The six steps

The first three steps run once, during indexing. The last three run for every
question.

1. Chunk your documents into small passages.
2. Embed each chunk into a vector.
3. Store the vectors in a vector store.
4. Embed the incoming question with the same embedding model.
5. Retrieve the chunks whose vectors are closest to the question.
6. Generate an answer by giving those chunks to the chat model as context.

## Chunking

Chunk size is a balance. Chunks that are too large bury the answer among
unrelated text and waste the model's attention. Chunks that are too small lose
the surrounding context that makes a passage meaningful. A few hundred words per
chunk is a sensible starting point. Adding a small overlap between consecutive
chunks, for example twenty words, keeps a sentence that straddles a boundary from
being split across two chunks in a way that loses its meaning.

## Grounding the answer

Retrieval only finds candidate passages. What keeps the model honest is the
system prompt. Tell the model to answer using only the provided context and to
say it does not know when the context does not contain the answer. Ask it to cite
the source of each fact. A strong system prompt sharply reduces hallucination,
although a small model can still drift, so retrieval quality matters more than
prompt wording.

## Common pitfalls

- Dimension mismatch between the embedding model and the vector property is the
  most common first error.
- Know whether your distance function treats higher or lower scores as better
  before you filter on the score.
- Embeddings are a snapshot. When a document changes you must re-embed and
  upsert it, or retrieval keeps returning the old version.
- Returning too many weak matches dilutes the context. Keep the top few and drop
  anything below a sensible similarity score.

## RAG versus fine-tuning

Fine-tuning teaches a model a style or skill and is a poor way to teach it facts,
because the facts are frozen into the weights and must be retrained when they
change. RAG keeps knowledge in a store you can edit at any time, and the model
reads the current version on every query. For question answering over changing
documents, start with RAG.
