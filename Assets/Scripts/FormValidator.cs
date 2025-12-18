using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FormValidator : MonoBehaviour
{
    // ================= ENUMS =================
    public enum FieldType
    {
        InputField,
        Dropdown,
        RadioGroup
    }

    public enum ValidationType
    {
        Required,
        MinLength,
        MaxLength,
        MobileNumber,
        Email,
        NoSpecialCharacters,
        NoNumber
    }

    public enum MessageType
    {
        Success,
        Error
    }

    // ================= DATA STRUCTURES =================
    [System.Serializable]
    public class ValidationRule
    {
        public ValidationType type;
        public string errorMessage;
        public int length;
    }

    [System.Serializable]
    public class FormField
    {
        public FieldType fieldType;

        [Header("Common")]
        public TMP_Text statusText;
        public List<ValidationRule> rules;

        [Header("Input Field")]
        public TMP_InputField inputField;

        [Header("Dropdown")]
        public TMP_Dropdown dropdown;

        [Header("Radio Group")]
        public ToggleGroup radioGroup;
    }

    [System.Serializable]
    public class Form
    {
        public string formName;

        [Header("Fields (Order Matters)")]
        public List<FormField> fields;

        [Header("Form Message")]
        public TMP_Text formMessageText;
        public string successMessage;

        [Header("Form Buttons")]
        public Button submitButton;

        [Header("Panel to activate on Success")]
        public GameObject selfPanel;
        public GameObject siblingPanel;
    }

    // ================= INSPECTOR =================
    [Header("ALL FORMS")]
    public List<Form> forms;

    [Header("Message Settings")]
    public float messageHideDelay = 3f;

    [Header("Colors")]
    public Color successColor = Color.green;
    public Color errorColor = Color.red;

    Coroutine messageCoroutine;

    // ================= PUBLIC API =================
    public void Validate(int formIndex)
    {
        if (ValidateForm(formIndex))
        {
            Debug.Log("Form validated successfully");
            Form form = forms[formIndex];
            form.siblingPanel.SetActive(true);
            form.selfPanel.SetActive(false);
        }
    }

    public bool ValidateForm(int formIndex)
    {
        if (formIndex < 0 || formIndex >= forms.Count)
            return false;

        Form form = forms[formIndex];
        ClearFormMessage(form);

        foreach (var field in form.fields)
        {
            // Stop on first invalid field
            if (!ValidateField(field))
                return false;
        }

        ShowFormMessage(form, form.successMessage, MessageType.Success);
        return true;
    }

    // ================= FIELD VALIDATION =================
    bool ValidateField(FormField field)
    {
        string value = GetFieldValue(field);

        foreach (var rule in field.rules)
        {
            if (!CheckRule(rule, value))
            {
                ShowFieldError(field, rule.errorMessage);
                return false;
            }
        }

        ClearFieldError(field);
        return true;
    }

    string GetFieldValue(FormField field)
    {
        switch (field.fieldType)
        {
            case FieldType.InputField:
                return field.inputField != null ? field.inputField.text : "";

            case FieldType.Dropdown:
                return field.dropdown != null && field.dropdown.value > 0
                    ? field.dropdown.options[field.dropdown.value].text
                    : "";

            case FieldType.RadioGroup:
                Toggle activeToggle = field.radioGroup.GetFirstActiveToggle();
                return activeToggle != null ? activeToggle.name : "";

            default:
                return "";
        }
    }

    // ================= RULE CHECK =================
    bool CheckRule(ValidationRule rule, string value)
    {
        switch (rule.type)
        {
            case ValidationType.Required:
                return !string.IsNullOrWhiteSpace(value);

            case ValidationType.MinLength:
                return value.Length >= rule.length;

            case ValidationType.MaxLength:
                return value.Length <= rule.length;

            case ValidationType.MobileNumber:
                return Regex.IsMatch(value, @"^[6-9]\d{9}$");

            case ValidationType.Email:
                return Regex.IsMatch(value,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

            case ValidationType.NoSpecialCharacters:
                return Regex.IsMatch(value, @"^[a-zA-Z0-9\s]+$");

            case ValidationType.NoNumber:
                return !Regex.IsMatch(value, @"\d");

            default:
                return true;
        }
    }

    // ================= UI HELPERS =================
    void ShowFieldError(FormField field, string message)
    {
        if (field.statusText == null) return;

        field.statusText.text = message;
        field.statusText.color = errorColor;
        field.statusText.gameObject.SetActive(true);

        StartCoroutine(AutoHideFieldError(field));
    }

    IEnumerator AutoHideFieldError(FormField field)
    {
        yield return new WaitForSeconds(messageHideDelay);
        ClearFieldError(field);
    }

    void ClearFieldError(FormField field)
    {
        if (field.statusText == null) return;

        field.statusText.text = "";
        field.statusText.gameObject.SetActive(false);
    }

    void ShowFormMessage(Form form, string message, MessageType type)
    {
        if (form.formMessageText == null) return;

        form.formMessageText.text = message;
        form.formMessageText.color = GetColor(type);
        form.formMessageText.gameObject.SetActive(true);

        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        messageCoroutine = StartCoroutine(AutoHideFormMessage(form));
    }

    IEnumerator AutoHideFormMessage(Form form)
    {
        yield return new WaitForSeconds(messageHideDelay);
        ClearFormMessage(form);
    }

    void ClearFormMessage(Form form)
    {
        if (form.formMessageText == null) return;

        form.formMessageText.text = "";
        form.formMessageText.gameObject.SetActive(false);
    }

    Color GetColor(MessageType type)
    {
        return type == MessageType.Success ? successColor : errorColor;
    }
}
