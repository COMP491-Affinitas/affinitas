import os
from pymongo import MongoClient
from dotenv import load_dotenv

# Load variables
def load_config():
    load_dotenv()
    return os.getenv("MONGODB_URI"), os.getenv("MONGODB_DBNAME")

# Get connection
def get_db_connection(uri, dbname):
    client = MongoClient(uri)
    db = client[dbname]
    return client, db

# Verify connection
def test_connection(client):
    try:
        client.admin.command('ping')
        print("MongoDB connected successfully.")
    except Exception as e:
        print("MongoDB connection failed:", e)

# Insert one collection to mongodb
def insert_document(db, collection_name, document):
    try:
        collection = db[collection_name]
        result = collection.insert_one(document)
        print("Insert successful, document id:", result.inserted_id)
        return result.inserted_id
    except Exception as e:
        print("Insert failed:", e)
        return None

# Retrieve a collection from mongodb
def retrieve_documents(db, collection_name, query):
    try:
        collection = db[collection_name]
        documents = list(collection.find(query))
        print("Retrieved documents:", documents)
        return documents
    except Exception as e:
        print("Retrieve failed:", e)
        return []
