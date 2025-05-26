/*
  # Initial Database Schema

  1. New Tables
    - users
      - id (uuid, primary key)
      - username (text, unique)
      - password (text)
      - email (text, unique)
      - full_name (text)
      - created_at (timestamptz)
      - last_login_at (timestamptz)
      - is_active (boolean)
    
    - folders
      - id (uuid, primary key)
      - user_id (uuid, references users)
      - name (text)
      - created_at (timestamptz)
      - updated_at (timestamptz)
    
    - file_types
      - id (serial, primary key)
      - name (text, unique)
    
    - output_formats
      - id (serial, primary key)
      - name (text, unique)
    
    - history
      - id (uuid, primary key)
      - user_id (uuid, references users)
      - process_date (timestamptz)
      - input_file_type_id (int, references file_types)
      - output_format_id (int, references output_formats)
      - processing_time (int)
      - prompt_text (text)
      - process_type (text)
      - is_success (boolean)
    
    - output_files
      - id (uuid, primary key)
      - history_id (uuid, references history)
      - name (text)
      - path (text)
      - size (bigint)
      - created_at (timestamptz)
      - folder_id (uuid, references folders)
    
    - charts
      - id (uuid, primary key)
      - user_id (uuid, references users)
      - type (text)
      - name (text)
      - last_generated_at (timestamptz)

  2. Security
    - Enable RLS on all tables
    - Add policies for authenticated users

  3. Initial Data
    - Insert default file types
    - Insert default output formats
*/

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create users table
CREATE TABLE users (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    username text UNIQUE NOT NULL,
    password text NOT NULL,
    email text UNIQUE NOT NULL,
    full_name text,
    created_at timestamptz DEFAULT now(),
    last_login_at timestamptz,
    is_active boolean DEFAULT true
);

-- Create folders table
CREATE TABLE folders (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id uuid NOT NULL REFERENCES users(id),
    name text NOT NULL,
    created_at timestamptz DEFAULT now(),
    updated_at timestamptz DEFAULT now(),
    CONSTRAINT fk_folders_user FOREIGN KEY (user_id) REFERENCES users(id)
);

-- Create file_types table
CREATE TABLE file_types (
    id serial PRIMARY KEY,
    name text UNIQUE NOT NULL
);

-- Create output_formats table
CREATE TABLE output_formats (
    id serial PRIMARY KEY,
    name text UNIQUE NOT NULL
);

-- Create history table
CREATE TABLE history (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id uuid NOT NULL REFERENCES users(id),
    process_date timestamptz DEFAULT now(),
    input_file_type_id integer NOT NULL REFERENCES file_types(id),
    output_format_id integer NOT NULL REFERENCES output_formats(id),
    processing_time integer,
    prompt_text text,
    process_type text,
    is_success boolean DEFAULT true,
    CONSTRAINT fk_history_user FOREIGN KEY (user_id) REFERENCES users(id),
    CONSTRAINT fk_history_file_type FOREIGN KEY (input_file_type_id) REFERENCES file_types(id),
    CONSTRAINT fk_history_output_format FOREIGN KEY (output_format_id) REFERENCES output_formats(id)
);

-- Create output_files table
CREATE TABLE output_files (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    history_id uuid NOT NULL REFERENCES history(id),
    name text NOT NULL,
    path text NOT NULL,
    size bigint,
    created_at timestamptz DEFAULT now(),
    folder_id uuid REFERENCES folders(id),
    CONSTRAINT fk_output_files_history FOREIGN KEY (history_id) REFERENCES history(id),
    CONSTRAINT fk_output_files_folder FOREIGN KEY (folder_id) REFERENCES folders(id)
);

-- Create charts table
CREATE TABLE charts (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id uuid NOT NULL REFERENCES users(id),
    type text NOT NULL,
    name text NOT NULL,
    last_generated_at timestamptz DEFAULT now(),
    CONSTRAINT fk_charts_user FOREIGN KEY (user_id) REFERENCES users(id)
);

-- Insert initial file types
INSERT INTO file_types (name) VALUES 
('PDF'), ('DOCX'), ('XLSX'), ('PNG'), ('JPG'), ('PROMPT'), ('OTHER');

-- Insert initial output formats
INSERT INTO output_formats (name) VALUES 
('Excel'), ('Word');

-- Enable Row Level Security (RLS)
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE folders ENABLE ROW LEVEL SECURITY;
ALTER TABLE history ENABLE ROW LEVEL SECURITY;
ALTER TABLE output_files ENABLE ROW LEVEL SECURITY;
ALTER TABLE charts ENABLE ROW LEVEL SECURITY;

-- Create RLS Policies
CREATE POLICY "Users can view own data" ON users
    FOR SELECT USING (auth.uid() = id);

CREATE POLICY "Users can update own data" ON users
    FOR UPDATE USING (auth.uid() = id);

CREATE POLICY "Users can view own folders" ON folders
    FOR SELECT USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own folders" ON folders
    FOR INSERT WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own folders" ON folders
    FOR UPDATE USING (auth.uid() = user_id);

CREATE POLICY "Users can delete own folders" ON folders
    FOR DELETE USING (auth.uid() = user_id);

CREATE POLICY "Users can view own history" ON history
    FOR SELECT USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own history" ON history
    FOR INSERT WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can view own output files" ON output_files
    FOR SELECT USING (
        auth.uid() IN (
            SELECT user_id FROM history WHERE id = output_files.history_id
        )
    );

CREATE POLICY "Users can insert own output files" ON output_files
    FOR INSERT WITH CHECK (
        auth.uid() IN (
            SELECT user_id FROM history WHERE id = history_id
        )
    );

CREATE POLICY "Users can view own charts" ON charts
    FOR SELECT USING (auth.uid() = user_id);

CREATE POLICY "Users can manage own charts" ON charts
    FOR ALL USING (auth.uid() = user_id);

-- Create functions for stored procedures
CREATE OR REPLACE FUNCTION user_login(p_username text, p_password text)
RETURNS TABLE (
    user_id uuid,
    username text,
    email text,
    full_name text
) AS $$
BEGIN
    RETURN QUERY
    UPDATE users 
    SET last_login_at = now()
    WHERE username = p_username 
    AND password = p_password 
    AND is_active = true
    RETURNING id, username, email, full_name;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to add file to folder
CREATE OR REPLACE FUNCTION add_file_to_folder(p_file_id uuid, p_folder_id uuid)
RETURNS void AS $$
BEGIN
    UPDATE output_files SET folder_id = p_folder_id WHERE id = p_file_id;
    UPDATE folders SET updated_at = now() WHERE id = p_folder_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to create folder and optionally add file
CREATE OR REPLACE FUNCTION create_folder_and_add_file(
    p_user_id uuid,
    p_folder_name text,
    p_file_id uuid DEFAULT NULL
) RETURNS uuid AS $$
DECLARE
    v_folder_id uuid;
BEGIN
    INSERT INTO folders (user_id, name)
    VALUES (p_user_id, p_folder_name)
    RETURNING id INTO v_folder_id;
    
    IF p_file_id IS NOT NULL THEN
        PERFORM add_file_to_folder(p_file_id, v_folder_id);
    END IF;
    
    RETURN v_folder_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to get input file type stats
CREATE OR REPLACE FUNCTION get_input_file_type_stats(p_user_id uuid DEFAULT NULL)
RETURNS TABLE (
    file_type text,
    usage_count bigint
) AS $$
BEGIN
    IF p_user_id IS NULL THEN
        RETURN QUERY
        SELECT 
            ft.name,
            COUNT(h.id)::bigint
        FROM history h
        JOIN file_types ft ON h.input_file_type_id = ft.id
        GROUP BY ft.name
        ORDER BY COUNT(h.id) DESC;
    ELSE
        RETURN QUERY
        SELECT 
            ft.name,
            COUNT(h.id)::bigint
        FROM history h
        JOIN file_types ft ON h.input_file_type_id = ft.id
        WHERE h.user_id = p_user_id
        GROUP BY ft.name
        ORDER BY COUNT(h.id) DESC;
    END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to get output format stats
CREATE OR REPLACE FUNCTION get_output_format_stats(p_user_id uuid DEFAULT NULL)
RETURNS TABLE (
    output_format text,
    usage_count bigint
) AS $$
BEGIN
    IF p_user_id IS NULL THEN
        RETURN QUERY
        SELECT 
            of.name,
            COUNT(h.id)::bigint
        FROM history h
        JOIN output_formats of ON h.output_format_id = of.id
        GROUP BY of.name
        ORDER BY COUNT(h.id) DESC;
    ELSE
        RETURN QUERY
        SELECT 
            of.name,
            COUNT(h.id)::bigint
        FROM history h
        JOIN output_formats of ON h.output_format_id = of.id
        WHERE h.user_id = p_user_id
        GROUP BY of.name
        ORDER BY COUNT(h.id) DESC;
    END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;