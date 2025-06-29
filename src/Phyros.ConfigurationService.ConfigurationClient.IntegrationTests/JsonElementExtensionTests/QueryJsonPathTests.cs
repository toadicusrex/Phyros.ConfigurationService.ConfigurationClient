using FluentAssertions;

namespace Phyros.ConfigurationService.ConfigurationClient.IntegrationTests.JsonElementExtensionTests;

public class QueryJsonPathTests
{
	[Fact]
	public void ShouldGetProducts_Edges_Node_Id()
	{
		raw.QueryJsonPath("data:products:edges:0:node:featuredImage:id").Should()
			.BeEquivalentTo("gid://shopify/ProductImage/146345345339732");

		raw.QueryJsonPath("data:products:edges:1:node:featuredImage:originalSrc").Should()
			.BeEquivalentTo("https://cdn.shopify.com/s/files/1/0286/pic.jpg");
	}

	string raw = @"{
        ""data"": {
        ""products"": {
            ""edges"": [
                {
                    ""node"": {
                        ""id"": ""gid://shopify/Product/4534543543316"",
                        ""featuredImage"": {
                            ""originalSrc"": ""https://cdn.shopify.com/s/files/1/0286/pic.jpg"",
                            ""id"": ""gid://shopify/ProductImage/146345345339732""
                        }
                    }
                },
                {
                    ""node"": {
                        ""id"": ""gid://shopify/Product/123456789"",
                        ""featuredImage"": {
                            ""originalSrc"": ""https://cdn.shopify.com/s/files/1/0286/pic.jpg"",
                            ""id"": [
                                ""gid://shopify/ProductImage/123456789"",
                                ""gid://shopify/ProductImage/666666666""
                            ]
                        },
                        ""1"": {
                            ""name"": ""Tuanh""
                        }
                    }
                }
            ]
        }
        }
    }";

}
