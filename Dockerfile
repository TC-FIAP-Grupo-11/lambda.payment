FROM public.ecr.aws/lambda/dotnet:8 AS base

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/FCG.Lambda.Payment/FCG.Lambda.Payment.csproj .
RUN dotnet restore
COPY src/FCG.Lambda.Payment/ .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
COPY --from=build /app/publish ${LAMBDA_TASK_ROOT}
CMD ["FCG.Lambda.Payment::FCG.Lambda.Payment.Function::FunctionHandler"]
