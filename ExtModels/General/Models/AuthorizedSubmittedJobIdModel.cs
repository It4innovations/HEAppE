using FluentValidation;

namespace HEAppE.ExtModels.General.Models
{
    public class AuthorizedSubmittedJobIdModel
    {
        #region Properties
        public string SessionCode { get; set; }
        public long SubmittedJobInfoId { get; set; }
        #endregion
        #region Constructors
        public AuthorizedSubmittedJobIdModel()
        {

        }

        public AuthorizedSubmittedJobIdModel(string sessionCode, long submittedJobInfoId)
        {
            SessionCode = sessionCode;
            SubmittedJobInfoId = submittedJobInfoId;
        }
        #endregion
    }
    public class AuthorizedSubmittedJobIdModelValidator : AbstractValidator<AuthorizedSubmittedJobIdModel>
    {
        public AuthorizedSubmittedJobIdModelValidator()
        {
            RuleFor(x => x.SessionCode).IsSessionCode();
            RuleFor(x => x.SubmittedJobInfoId).NotEmpty().GreaterThan(0);
        }
    }
}