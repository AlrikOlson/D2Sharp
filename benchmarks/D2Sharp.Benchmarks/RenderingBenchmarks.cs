using BenchmarkDotNet.Attributes;
using D2Sharp;

namespace D2Sharp.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class RenderingBenchmarks
{
    private D2Wrapper? _wrapper;

    private const string SimpleScript = "A -> B";

    private const string ComplexScript = @"
direction: right

server: Web Server {
    shape: rectangle
    style.fill: lightblue
}

db: Database {
    shape: cylinder
    style.fill: lightgreen
}

cache: Redis Cache {
    shape: rectangle
    style.fill: orange
}

loadbalancer: Load Balancer {
    shape: rectangle
    style.fill: purple
}

users: Users {
    shape: person
    style.fill: yellow
}

users -> loadbalancer: HTTPS
loadbalancer -> server: HTTP
server -> db: SQL Queries
server -> cache: Cache Operations
db -> cache: Invalidate Cache
";

    private const string VeryComplexScript = @"
direction: down

# Frontend Layer
web: Frontend {
    react: React App {
        shape: rectangle
        style.fill: lightblue
    }
    mobile: Mobile App {
        shape: rectangle
        style.fill: lightblue
    }
}

# API Gateway
gateway: API Gateway {
    nginx: NGINX {
        shape: rectangle
        style.fill: green
    }
    auth: Auth Service {
        shape: rectangle
        style.fill: purple
    }
}

# Microservices
services: Backend Services {
    user_service: User Service {
        shape: rectangle
        style.fill: orange
    }
    order_service: Order Service {
        shape: rectangle
        style.fill: orange
    }
    payment_service: Payment Service {
        shape: rectangle
        style.fill: orange
    }
    notification_service: Notification Service {
        shape: rectangle
        style.fill: orange
    }
}

# Data Layer
data: Data Layer {
    postgres: PostgreSQL {
        shape: cylinder
        style.fill: lightgreen
    }
    mongodb: MongoDB {
        shape: cylinder
        style.fill: lightgreen
    }
    redis: Redis {
        shape: cylinder
        style.fill: red
    }
}

# Message Queue
queue: Message Queue {
    kafka: Apache Kafka {
        shape: rectangle
        style.fill: yellow
    }
}

# Connections
web.react -> gateway.nginx: API Requests
web.mobile -> gateway.nginx: API Requests
gateway.nginx -> gateway.auth: Authenticate
gateway.auth -> services.user_service: User Data
gateway.nginx -> services.order_service: Orders
gateway.nginx -> services.payment_service: Payments
services.order_service -> data.postgres: Store Orders
services.payment_service -> data.postgres: Store Payments
services.user_service -> data.mongodb: User Profiles
services.order_service -> queue.kafka: Order Events
services.payment_service -> queue.kafka: Payment Events
queue.kafka -> services.notification_service: Events
services.notification_service -> web.mobile: Push Notifications
data.redis -> services.user_service: Cache
data.redis -> services.order_service: Cache
";

    [GlobalSetup]
    public void Setup()
    {
        _wrapper = new D2Wrapper();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _wrapper?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public RenderResult RenderSimpleDiagram()
    {
        return _wrapper!.RenderDiagram(SimpleScript);
    }

    [Benchmark]
    public RenderResult RenderComplexDiagram()
    {
        return _wrapper!.RenderDiagram(ComplexScript);
    }

    [Benchmark]
    public RenderResult RenderVeryComplexDiagram()
    {
        return _wrapper!.RenderDiagram(VeryComplexScript);
    }

    [Benchmark]
    public RenderResult RenderSimpleWithTheme()
    {
        return _wrapper!.RenderDiagram(SimpleScript, new RenderOptions { ThemeId = 1 });
    }

    [Benchmark]
    public RenderResult RenderSimpleWithSketch()
    {
        return _wrapper!.RenderDiagram(SimpleScript, new RenderOptions { Sketch = true });
    }

    [Benchmark]
    public RenderResult RenderComplexWithElk()
    {
        return _wrapper!.RenderDiagram(ComplexScript, new RenderOptions { Layout = LayoutEngine.Elk });
    }

    [Benchmark]
    public RenderResult RenderWithAllOptions()
    {
        return _wrapper!.RenderDiagram(ComplexScript, new RenderOptions
        {
            Layout = LayoutEngine.Elk,
            ThemeId = 1,
            Sketch = true,
            Pad = 75,
            Scale = 0.8,
            Center = true
        });
    }

    [Benchmark]
    public async Task<RenderResult> RenderSimpleDiagramAsync()
    {
        return await _wrapper!.RenderDiagramAsync(SimpleScript);
    }

    [Benchmark]
    public async Task<RenderResult> RenderComplexDiagramAsync()
    {
        return await _wrapper!.RenderDiagramAsync(ComplexScript);
    }
}
