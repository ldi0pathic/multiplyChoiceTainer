using System.ComponentModel;

namespace DAL.Model.Enum;

public enum QuestionType
{
    [Description("Auswahl (A)")] Auswahl,
    [Description("Pick (P)")] Pick,
    [Description("Kreuz (K)")] Kreuz
}