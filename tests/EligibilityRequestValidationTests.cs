using ClaimMD.Client.Models;
using FluentAssertions;
using Xunit;

namespace ClaimMD.Client.Tests;

public class EligibilityRequestValidationTests
{
    [Fact]
    public void EligibilityRequest_Defaults_PatientRelationship_ToSelf()
    {
        var req = new EligibilityRequest();
        req.PatientRelationship.Should().Be("18");
    }

    [Fact]
    public void EligibilityRequest_Defaults_ServiceTypeCode_ToHealthBenefit()
    {
        var req = new EligibilityRequest();
        req.ServiceTypeCode.Should().Be("30");
    }

    [Fact]
    public void ApiError_ToString_IncludesFieldWhenPresent()
    {
        var error = new ApiError { Code = "E001", Message = "Required", Field = "ins_last" };
        error.ToString().Should().Contain("ins_last");
        error.ToString().Should().Contain("E001");
    }

    [Fact]
    public void ApiError_ToString_OmitsFieldWhenAbsent()
    {
        var error = new ApiError { Code = "E002", Message = "Unauthorized" };
        error.ToString().Should().NotContain(":");
        error.ToString().Should().Contain("E002");
    }

    [Fact]
    public void ClaimAcknowledgment_IsAcknowledged_TrueForStatusA()
    {
        var ack = new ClaimAcknowledgment { Status = "A" };
        ack.IsAcknowledged.Should().BeTrue();
    }

    [Fact]
    public void ClaimAcknowledgment_IsAcknowledged_FalseForStatusR()
    {
        var ack = new ClaimAcknowledgment { Status = "R" };
        ack.IsAcknowledged.Should().BeFalse();
    }

    [Fact]
    public void ClaimStatusDetail_StatusDescription_ReturnsHumanReadable()
    {
        new ClaimStatusDetail { Status = "A" }.StatusDescription.Should().Be("Acknowledged");
        new ClaimStatusDetail { Status = "R" }.StatusDescription.Should().Be("Rejected");
        new ClaimStatusDetail { Status = "D" }.StatusDescription.Should().Be("Denied");
        new ClaimStatusDetail { Status = "P" }.StatusDescription.Should().Be("Paid");
        new ClaimStatusDetail { Status = "F" }.StatusDescription.Should().Be("Forwarded");
        new ClaimStatusDetail { Status = "X" }.StatusDescription.Should().Be("X");
        new ClaimStatusDetail { Status = null }.StatusDescription.Should().Be("Unknown");
    }

    [Fact]
    public void EligibilityResponse_IsSuccess_TrueWhenNoErrors()
    {
        var resp = new EligibilityResponse { Errors = null };
        resp.IsSuccess.Should().BeTrue();

        resp.Errors = new List<ApiError>();
        resp.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void EligibilityResponse_IsSuccess_FalseWhenErrorsPresent()
    {
        var resp = new EligibilityResponse
        {
            Errors = new List<ApiError> { new ApiError { Code = "E001", Message = "Bad request" } }
        };
        resp.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void PayerInfo_SupportsEligibility_CaseInsensitive()
    {
        new PayerInfo { EligibilitySupported = "Y" }.SupportsEligibility.Should().BeTrue();
        new PayerInfo { EligibilitySupported = "y" }.SupportsEligibility.Should().BeTrue();
        new PayerInfo { EligibilitySupported = "N" }.SupportsEligibility.Should().BeFalse();
        new PayerInfo { EligibilitySupported = null }.SupportsEligibility.Should().BeFalse();
    }

    [Fact]
    public void ClaimMdApiException_Message_CombinesAllErrors()
    {
        var errors = new[]
        {
            new ApiError { Code = "E001", Message = "Missing NPI" },
            new ApiError { Code = "E002", Message = "Invalid DOB" }
        };
        var ex = new ClaimMdApiException(errors);
        ex.Message.Should().Contain("Missing NPI");
        ex.Message.Should().Contain("Invalid DOB");
        ex.Errors.Should().HaveCount(2);
    }
}
