using PrivacyGateway.Api.Pipeline.Abstractions;
using PrivacyGateway.Api.Pipeline.Context;

namespace PrivacyGateway.Api.Pipeline.Steps;

// ExtractTextStep er det første step i pipelinen, og har ansvar for at validere og udtrække tekst fra den rå input.
// I denne MVP-version antager jeg, at den rå input allerede er tekst, så vi sætter ExtractedText direkte til RawInput.
// I fremtiden kan dette step udvides til at håndtere forskellige inputtyper (filer, billeder, osv.) og bruge OCR eller andre teknikker til at udtrække tekst.
public sealed class ExtractTextStep : IPipelineStep
{
    public string Name => "ExtractText";

    // ExtractTextStep skal køre tidligt i pipelinen, så den har lav order. Den skal også køre for både Analyze og Chat pipelines, så den har AppliesTo for begge.
    public int Order => 10;

    // AppliesTo er en bitflag, så jeg kan kombinere Analyze og Chat med en bitwise OR
    public PipelineMode AppliesTo => PipelineMode.Analyze | PipelineMode.Chat;

    // ExecuteAsync validerer at der er rå input, og sætter ExtractedText til den rå input. Hvis der ikke er rå input, fejler den pipelinen med en 400 Bad Request.
    public Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        // Valider at der er rå input
        if (string.IsNullOrWhiteSpace(context.RawInput))
        {
            context.Fail(400, "Text is required.");
            return Task.CompletedTask;
        }

        // MVP: tekst-input -> extracted text
        context.ExtractedText = context.RawInput;
        return Task.CompletedTask;
    }
}