# FCG.Lambda.Payment

**Tech Challenge - Fase 3**
AWS Lambda para processamento de pagamentos da plataforma FIAP Cloud Games.

## Responsabilidade

Processar eventos de `OrderPlacedEvent` — simula o processamento de pagamento e retorna o resultado.

> Esta Lambda é invocada diretamente pelo `FCG.Api.Payments` via AWS SDK (não por SQS trigger), já que o RabbitMQ rodando no EKS não pode acionar Lambdas nativamente.

## Estrutura

```
FCG.Lambda.Payment/
├── src/
│   └── FCG.Lambda.Payment/       # Handler principal
│       ├── Function.cs            # Entrypoint Lambda
│       └── Dockerfile
└── test/
    └── FCG.Lambda.Payment.Tests/ # Testes unitários
```

## Executar localmente (simulação)

```bash
cd src/FCG.Lambda.Payment
dotnet run
```

## Testes

```bash
dotnet test test/FCG.Lambda.Payment.Tests/FCG.Lambda.Payment.Tests.csproj
```

## Docker (imagem para ECR)

```bash
docker build -t fcg-lambda-payment .
```

## Deploy na AWS

O deploy é feito automaticamente pelo pipeline CD (`.github/workflows/cd.yml`):
1. Build da imagem Docker
2. Push para ECR (`fcg-lambda-payment`)
3. `aws lambda update-function-code --function-name fcg-payment-processor --image-uri <ecr-uri>`

Para deploy manual via Terraform:
```bash
cd FCG.Infra.Orchestration/terraform
terraform apply -target=module.lambda
```

## CI/CD (GitHub Actions)

- **CI** (`.github/workflows/ci.yml`): build + testes em push/PR na `main`
- **CD** (`.github/workflows/cd.yml`): build Docker → push ECR → atualiza Lambda

**Secrets obrigatórios no repositório GitHub:**
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`

## Variáveis de Ambiente (Lambda)

| Variável | Descrição |
|----------|-----------|
| `AWS_REGION` | Região AWS (configurado no Terraform) |
