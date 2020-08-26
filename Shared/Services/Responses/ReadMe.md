This folder defined the response classes in REST from the Refit api https://github.com/reactiveui/refit
It's parsed from json automatically when receive the response from server.
The response class is used in case there are many information in the server response, but we only need the child model in json structure
Example:

```
public interface IService
{
    [Get("/docs/{id}")]
    Task<OutputDocument> GetDocument(string id);
}

// example OutputDocument
class OutputDocument
{
    public Container Container {get; set;}

    public class Container
    {
         public Document Doc {get; set;}
    }
}
```