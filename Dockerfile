FROM public.ecr.aws/lambda/dotnet:8
COPY publish/ ${LAMBDA_TASK_ROOT}
CMD ["FCG.Lambda.Payment::FCG.Lambda.Payment.Function::FunctionHandler"]
