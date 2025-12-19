using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class OtpInputController : MonoBehaviour
{
    [Tooltip("OTP input fields in order (left to right).")]
    public TMP_InputField[] fields;

    public Action<string> OnOtpCompleted;

    private int _focusedIndex = -1;
    private bool _isHandlingPaste;

    private void Awake()
    {
        if (fields == null || fields.Length == 0)
        {
            Debug.LogWarning("OtpInputController: No fields assigned.");
            return;
        }

        for (int i = 0; i < fields.Length; i++)
        {
            var idx = i;

            // Ensure single-character constraint in each field.
            fields[i].characterLimit = 1;
            fields[i].contentType = TMP_InputField.ContentType.IntegerNumber;

            fields[i].onSelect.AddListener(_ => _focusedIndex = idx);

            fields[i].onValueChanged.AddListener(value =>
            {
                // Handle paste of multiple digits (desktop).
                if (!_isHandlingPaste && value != null && value.Length > 1)
                {
                    DistributePaste(value, idx);
                    return;
                }

                // If user entered a digit, keep only the last digit and advance.
                if (!string.IsNullOrEmpty(value))
                {
                    var last = value[value.Length - 1];
                    if (char.IsDigit(last))
                    {
                        // Normalize field to single last digit.
                        if (fields[idx].text != last.ToString())
                            fields[idx].SetTextWithoutNotify(last.ToString());

                        // Move to next field if any.
                        FocusNext(idx);
                    }
                    else
                    {
                        // Remove non-digit.
                        fields[idx].SetTextWithoutNotify(string.Empty);
                        fields[idx].ActivateInputField();
                        fields[idx].caretPosition = 0;
                    }
                }
            });

            // Intercept input to ensure only digits.
            fields[i].onValidateInput += (text, charIndex, addedChar) =>
            {
                return char.IsDigit(addedChar) ? addedChar : '\0';
            };
        }
    }

    private void Update()
    {
        if (fields == null || fields.Length == 0)
            return;

        // Track focus changes from EventSystem.
        var focused = GetFocusedFieldIndex();
        if (focused != -1)
            _focusedIndex = focused;

        // Backspace behavior.
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (_focusedIndex == -1)
                _focusedIndex = GetFocusedFieldIndex();

            if (_focusedIndex == -1)
                return;

            var field = fields[_focusedIndex];
            if (!string.IsNullOrEmpty(field.text))
            {
                // Clear current and keep focus.
                field.SetTextWithoutNotify(string.Empty);
                field.ActivateInputField();
                field.caretPosition = 0;
            }
            else
            {
                // Move to previous, clear it, keep focus there.
                var prev = _focusedIndex - 1;
                if (prev >= 0)
                {
                    var prevField = fields[prev];
                    prevField.SetTextWithoutNotify(string.Empty);
                    ActivateField(prev);
                }
            }
        }

        // Enter moves to next empty or triggers complete (optional)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (IsComplete())
            {
                var otp = GetOtp();
                OnOtpCompleted?.Invoke(otp);
            }
            else
            {
                FocusNextEmpty();
            }
        }
    }

    private void FocusNext(int index)
    {
        var next = index + 1;
        if (next < fields.Length)
        {
            ActivateField(next);
        }
        else
        {
            // Last field filled; keep caret at end and optionally notify.
            fields[index].DeactivateInputField();
            if (IsComplete())
            {
                var otp = GetOtp();
                OnOtpCompleted?.Invoke(otp);
            }
        }
    }

    private void FocusNextEmpty()
    {
        for (int i = 0; i < fields.Length; i++)
        {
            if (string.IsNullOrEmpty(fields[i].text))
            {
                ActivateField(i);
                return;
            }
        }
        // All filled: focus last.
        ActivateField(fields.Length - 1);
    }

    private void ActivateField(int index)
    {
        _focusedIndex = index;
        var f = fields[index];
        EventSystem.current?.SetSelectedGameObject(f.gameObject);
        f.ActivateInputField();
        f.caretPosition = string.IsNullOrEmpty(f.text) ? 0 : 1;
    }

    private void DistributePaste(string pasted, int startIndex)
    {
        _isHandlingPaste = true;

        var digits = pasted.Where(char.IsDigit).ToArray();
        if (digits.Length == 0)
        {
            fields[startIndex].SetTextWithoutNotify(string.Empty);
            ActivateField(startIndex);
            _isHandlingPaste = false;
            return;
        }

        int idx = startIndex;
        for (int i = 0; i < digits.Length && idx < fields.Length; i++, idx++)
        {
            fields[idx].SetTextWithoutNotify(digits[i].ToString());
        }

        // Clear remaining fields after paste to avoid stale data if desired.
        for (int j = idx; j < fields.Length; j++)
        {
            fields[j].SetTextWithoutNotify(string.Empty);
        }

        // Focus next empty or last.
        if (idx < fields.Length)
            ActivateField(idx);
        else
            ActivateField(fields.Length - 1);

        _isHandlingPaste = false;
    }

    private int GetFocusedFieldIndex()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
            return -1;

        var go = EventSystem.current.currentSelectedGameObject;
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i] != null && fields[i].gameObject == go)
                return i;
        }
        return -1;
    }

    private bool IsComplete()
    {
        if (fields == null || fields.Length == 0)
            return false;
        for (int i = 0; i < fields.Length; i++)
        {
            if (string.IsNullOrEmpty(fields[i].text))
                return false;
        }
        return true;
    }

    public string GetOtp()
    {
        return string.Concat(fields.Select(f => string.IsNullOrEmpty(f.text) ? "" : f.text));
    }

    public void ClearAll()
    {
        foreach (var f in fields)
            f.SetTextWithoutNotify(string.Empty);
        ActivateField(0);
    }
}