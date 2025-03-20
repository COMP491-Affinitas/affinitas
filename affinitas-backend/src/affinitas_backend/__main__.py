import sys
from time import perf_counter

from transformers import AutoTokenizer, LlamaForCausalLM


model_dir = sys.argv[1]

t1 = perf_counter()

tokenizer = AutoTokenizer.from_pretrained(model_dir, trust_remote_code=True)
model = LlamaForCausalLM.from_pretrained(model_dir)

t2 = perf_counter()

print(t2 - t1)

prompt = "Generate a story for a 2D RPG game."

t1 = perf_counter()

inputs = tokenizer(prompt, return_tensors="pt")
inputs = {k: v.to(model.device) for k, v in inputs.items()}

outputs = model.generate(**inputs, max_new_tokens=1000, do_sample=True, temperature=0.7)
generated_text = tokenizer.decode(outputs[0], skip_special_tokens=True)

t2 = perf_counter()

print(t2 - t1)
print(generated_text)
