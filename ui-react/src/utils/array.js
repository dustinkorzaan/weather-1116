export function findLastIndex(array, predicate) {
  for (let index = array.length - 1; index >= 0; index -= 1) {
    if (predicate(array[index], index, array)) {
      return index;
    }
  }

  return -1;
}
