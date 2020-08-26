This folder defined the input parameter classes in REST request in Refit api https://github.com/reactiveui/refit
It's parsed into json or xml automatically when sending to server.

Example:

```
public interface IService
{
    [Get("/docs/{input}")]
    Task<Document> GetDocument(InputParam input);
}
```