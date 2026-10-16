# DocIntake test data

24 synthetic documents (6 per type). Every document carries a "synthetic sample" footer; all names, numbers and addresses are invented.

- Documents 01-04 of each type are internally consistent (expectedIssues is empty).
- Documents 05-06 contain deliberate flaws. The answer key records the values *as printed* (including the flaw) and lists the validation issues the rules should raise.
- `*.answer.json`: documentType, expected (matches `schemas/<type>.schema.json`), expectedIssues (rule + field path).
- `manifest.json`: index of all documents.
- Dates in answers are ISO 8601; amounts are numbers; a field not shown on the document would be null.
