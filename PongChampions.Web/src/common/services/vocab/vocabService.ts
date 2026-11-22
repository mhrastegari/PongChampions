import { vocabs } from '@shared/vocabs';

let _vocabs: Record<string, string> = {};

// The API does not serve translations yet; when it does, fetch the dictionary here.
function init() {
  _vocabs = { ...vocabs };
}

// A missing entry returns its key, so the gap is visible instead of blank.
function get(key: string): string {
  return _vocabs[key] ?? key;
}

function format(key: string, ...placeholders: (string | number)[]): string {
  let word = get(key);

  for (const placeholder of placeholders) {
    word = word.replace('%', String(placeholder));
  }

  return word;
}

export const vocabService = {
  init,
  get,
  format,
};
