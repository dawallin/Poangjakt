using Poangjakten.Web.Participants;
using Poangjakten.Web.PartyStages;
using Poangjakten.Web.Scoring;

namespace Poangjakten.Web.SpecialQuestions;

public static class SpecialQuestionEndpoints
{
    public static IEndpointRouteBuilder MapSpecialQuestionEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/participants/{participantId}/special-questions", (
            string participantId,
            ParticipantRegistry participants,
            SpecialAnswerRegistry answers,
            PartyStageRegistry stages) =>
        {
            if (participants.Find(participantId) is null) return Results.NotFound();

            return Results.Ok(SpecialQuestionDefinitions.All
                .Where(question => stages.IsUnlocked(question.UnlockStageId))
                .Select(question => SpecialQuestionResponse.From(
                    question,
                    answers.Find(participantId, question.Id))));
        });

        routes.MapPut("/api/participants/{participantId}/special-questions/{questionId}", async (
            string participantId,
            string questionId,
            SaveSpecialAnswerRequest request,
            ParticipantRegistry participants,
            SpecialAnswerRegistry answers,
            PartyStageRegistry stages,
            ScoreService scores,
            CancellationToken cancellationToken) =>
        {
            var participant = participants.Find(participantId);
            var question = SpecialQuestionDefinitions.Find(questionId);
            if (participant is null || question is null) return Results.NotFound();
            if (!stages.IsUnlocked(question.UnlockStageId)) return Results.NotFound();

            var result = await answers.SetAsync(participantId, questionId, request.Value, cancellationToken);
            if (result.Answer is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["specialQuestion"] = [result.Error ?? "Svaret är ogiltigt."]
                });
            }

            return Results.Ok(new SaveSpecialAnswerResponse(
                question.Id,
                result.Answer.Value,
                question.PointsFor(result.Answer.Value),
                scores.GetScore(participant)));
        });

        routes.MapDelete("/api/participants/{participantId}/special-questions/{questionId}", async (
            string participantId,
            string questionId,
            ParticipantRegistry participants,
            SpecialAnswerRegistry answers,
            CancellationToken cancellationToken) =>
        {
            if (participants.Find(participantId) is null ||
                SpecialQuestionDefinitions.Find(questionId) is null)
                return Results.NotFound();

            return await answers.RemoveAsync(participantId, questionId, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        });

        return routes;
    }
}

public sealed record SaveSpecialAnswerRequest(int Value);
public sealed record SaveSpecialAnswerResponse(string Id, int Value, int Points, int Score);

public sealed record SpecialQuestionResponse(string Id, string Prompt, int? Value, int Points)
{
    public static SpecialQuestionResponse From(
        SpecialQuestionDefinition question,
        SpecialAnswer? answer) =>
        new(
            question.Id,
            question.Prompt,
            answer?.Value,
            answer is null ? 0 : question.PointsFor(answer.Value));
}
