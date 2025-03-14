import sys

from transformers import AutoTokenizer, LlamaForCausalLM


model_dir = sys.argv[1]

tokenizer = AutoTokenizer.from_pretrained(model_dir, trust_remote_code=True)
model = LlamaForCausalLM.from_pretrained(model_dir)

prompt = "Generate a story for a 2D RPG game."

inputs = tokenizer(prompt, return_tensors="pt")
inputs = {k: v.to(model.device) for k, v in inputs.items()}

outputs = model.generate(**inputs, max_new_tokens=1000, do_sample=True, temperature=0.7)
generated_text = tokenizer.decode(outputs[0], skip_special_tokens=True)
print(generated_text)
