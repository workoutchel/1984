--
-- PostgreSQL database dump
--

\restrict xA6NWBTv1atYBXtYFtwnMgM01Vb9q8J6xndrqrvLlSuOzzuuaxIqzAMEKaTIGlb

-- Dumped from database version 18.0
-- Dumped by pg_dump version 18.0

-- Started on 2026-05-06 01:43:09

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

DROP DATABASE "1984";
--
-- TOC entry 5165 (class 1262 OID 16515)
-- Name: 1984; Type: DATABASE; Schema: -; Owner: postgres
--

CREATE DATABASE "1984" WITH TEMPLATE = template0 ENCODING = 'UTF8' LOCALE_PROVIDER = libc LOCALE = 'Russian_Russia.1251';


ALTER DATABASE "1984" OWNER TO postgres;

\unrestrict xA6NWBTv1atYBXtYFtwnMgM01Vb9q8J6xndrqrvLlSuOzzuuaxIqzAMEKaTIGlb
\connect "1984"
\restrict xA6NWBTv1atYBXtYFtwnMgM01Vb9q8J6xndrqrvLlSuOzzuuaxIqzAMEKaTIGlb

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 222 (class 1259 OID 16530)
-- Name: activity_events; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.activity_events (
    id integer NOT NULL,
    workstation_id integer NOT NULL,
    last_active_time timestamp without time zone NOT NULL
);


ALTER TABLE public.activity_events OWNER TO postgres;

--
-- TOC entry 221 (class 1259 OID 16529)
-- Name: activity_events_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.activity_events_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.activity_events_id_seq OWNER TO postgres;

--
-- TOC entry 5166 (class 0 OID 0)
-- Dependencies: 221
-- Name: activity_events_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.activity_events_id_seq OWNED BY public.activity_events.id;


--
-- TOC entry 242 (class 1259 OID 32902)
-- Name: activity_period_types; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.activity_period_types (
    id integer NOT NULL,
    name character varying(20) NOT NULL
);


ALTER TABLE public.activity_period_types OWNER TO postgres;

--
-- TOC entry 241 (class 1259 OID 32901)
-- Name: activity_period_types_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.activity_period_types_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.activity_period_types_id_seq OWNER TO postgres;

--
-- TOC entry 5167 (class 0 OID 0)
-- Dependencies: 241
-- Name: activity_period_types_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.activity_period_types_id_seq OWNED BY public.activity_period_types.id;


--
-- TOC entry 224 (class 1259 OID 16545)
-- Name: activity_periods; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.activity_periods (
    id integer NOT NULL,
    workstation_id integer NOT NULL,
    start_time timestamp without time zone NOT NULL,
    end_time timestamp without time zone,
    duration_seconds integer,
    period_type_id integer NOT NULL
);


ALTER TABLE public.activity_periods OWNER TO postgres;

--
-- TOC entry 223 (class 1259 OID 16544)
-- Name: activity_periods_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.activity_periods_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.activity_periods_id_seq OWNER TO postgres;

--
-- TOC entry 5168 (class 0 OID 0)
-- Dependencies: 223
-- Name: activity_periods_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.activity_periods_id_seq OWNED BY public.activity_periods.id;


--
-- TOC entry 238 (class 1259 OID 16735)
-- Name: application_rules; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.application_rules (
    id integer NOT NULL,
    application_name character varying(255) NOT NULL,
    rule_type_id integer NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


ALTER TABLE public.application_rules OWNER TO postgres;

--
-- TOC entry 237 (class 1259 OID 16734)
-- Name: application_rules_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.application_rules_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.application_rules_id_seq OWNER TO postgres;

--
-- TOC entry 5169 (class 0 OID 0)
-- Dependencies: 237
-- Name: application_rules_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.application_rules_id_seq OWNED BY public.application_rules.id;


--
-- TOC entry 230 (class 1259 OID 16601)
-- Name: dns_cache_records; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.dns_cache_records (
    id integer NOT NULL,
    workstation_id integer NOT NULL,
    domain_name character varying(255) NOT NULL,
    resolved_ip character varying(45),
    record_time timestamp without time zone NOT NULL
);


ALTER TABLE public.dns_cache_records OWNER TO postgres;

--
-- TOC entry 229 (class 1259 OID 16600)
-- Name: dns_cache_records_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.dns_cache_records_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.dns_cache_records_id_seq OWNER TO postgres;

--
-- TOC entry 5170 (class 0 OID 0)
-- Dependencies: 229
-- Name: dns_cache_records_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.dns_cache_records_id_seq OWNED BY public.dns_cache_records.id;


--
-- TOC entry 244 (class 1259 OID 41093)
-- Name: reports; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.reports (
    id integer NOT NULL,
    workstation_id integer NOT NULL,
    period_start date NOT NULL,
    period_end date NOT NULL,
    report_type character varying(50) DEFAULT 'daily_activity_report'::character varying NOT NULL,
    report_data jsonb NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


ALTER TABLE public.reports OWNER TO postgres;

--
-- TOC entry 243 (class 1259 OID 41092)
-- Name: reports_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.reports_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.reports_id_seq OWNER TO postgres;

--
-- TOC entry 5171 (class 0 OID 0)
-- Dependencies: 243
-- Name: reports_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.reports_id_seq OWNED BY public.reports.id;


--
-- TOC entry 236 (class 1259 OID 16724)
-- Name: rule_types; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.rule_types (
    id integer NOT NULL,
    name character varying(20) NOT NULL
);


ALTER TABLE public.rule_types OWNER TO postgres;

--
-- TOC entry 235 (class 1259 OID 16723)
-- Name: rule_types_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.rule_types_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.rule_types_id_seq OWNER TO postgres;

--
-- TOC entry 5172 (class 0 OID 0)
-- Dependencies: 235
-- Name: rule_types_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.rule_types_id_seq OWNED BY public.rule_types.id;


--
-- TOC entry 232 (class 1259 OID 16647)
-- Name: screenshots; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.screenshots (
    id integer NOT NULL,
    workstation_id integer NOT NULL,
    file_path text NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


ALTER TABLE public.screenshots OWNER TO postgres;

--
-- TOC entry 231 (class 1259 OID 16646)
-- Name: screenshots_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.screenshots_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.screenshots_id_seq OWNER TO postgres;

--
-- TOC entry 5173 (class 0 OID 0)
-- Dependencies: 231
-- Name: screenshots_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.screenshots_id_seq OWNED BY public.screenshots.id;


--
-- TOC entry 234 (class 1259 OID 16666)
-- Name: violations; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.violations (
    id integer NOT NULL,
    workstation_id integer NOT NULL,
    source_table character varying(50),
    source_event_id integer,
    violation_type character varying(50) NOT NULL,
    severity character varying(20) DEFAULT 'medium'::character varying NOT NULL,
    description text NOT NULL,
    related_entity text,
    rule_source character varying(100),
    detected_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    is_resolved boolean DEFAULT false NOT NULL,
    resolved_at timestamp without time zone,
    resolution_comment text,
    CONSTRAINT violations_severity_check CHECK (((severity)::text = ANY ((ARRAY['low'::character varying, 'medium'::character varying, 'high'::character varying, 'critical'::character varying])::text[]))),
    CONSTRAINT violations_violation_type_check CHECK (((violation_type)::text = ANY ((ARRAY['blacklist_application'::character varying, 'blacklist_web_resource'::character varying, 'long_idle'::character varying])::text[])))
);


ALTER TABLE public.violations OWNER TO postgres;

--
-- TOC entry 233 (class 1259 OID 16665)
-- Name: violations_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.violations_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.violations_id_seq OWNER TO postgres;

--
-- TOC entry 5174 (class 0 OID 0)
-- Dependencies: 233
-- Name: violations_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.violations_id_seq OWNED BY public.violations.id;


--
-- TOC entry 228 (class 1259 OID 16581)
-- Name: web_activity; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.web_activity (
    id integer NOT NULL,
    workstation_id integer NOT NULL,
    process_name character varying(255),
    window_title text,
    domain_name character varying(255) NOT NULL,
    detection_method character varying(50) NOT NULL,
    access_time timestamp without time zone NOT NULL,
    CONSTRAINT web_activity_detection_method_check CHECK (((detection_method)::text = ANY ((ARRAY['window_title'::character varying, 'dns_cache'::character varying, 'combined'::character varying])::text[])))
);


ALTER TABLE public.web_activity OWNER TO postgres;

--
-- TOC entry 227 (class 1259 OID 16580)
-- Name: web_activity_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.web_activity_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.web_activity_id_seq OWNER TO postgres;

--
-- TOC entry 5175 (class 0 OID 0)
-- Dependencies: 227
-- Name: web_activity_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.web_activity_id_seq OWNED BY public.web_activity.id;


--
-- TOC entry 240 (class 1259 OID 16754)
-- Name: web_resource_rules; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.web_resource_rules (
    id integer NOT NULL,
    domain_name character varying(255) NOT NULL,
    rule_type_id integer NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


ALTER TABLE public.web_resource_rules OWNER TO postgres;

--
-- TOC entry 239 (class 1259 OID 16753)
-- Name: web_resource_rules_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.web_resource_rules_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.web_resource_rules_id_seq OWNER TO postgres;

--
-- TOC entry 5176 (class 0 OID 0)
-- Dependencies: 239
-- Name: web_resource_rules_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.web_resource_rules_id_seq OWNED BY public.web_resource_rules.id;


--
-- TOC entry 226 (class 1259 OID 16562)
-- Name: window_activity; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.window_activity (
    id integer NOT NULL,
    workstation_id integer NOT NULL,
    window_title text NOT NULL,
    process_name character varying(255) NOT NULL,
    process_id integer,
    start_time timestamp without time zone NOT NULL,
    end_time timestamp without time zone,
    duration_seconds integer
);


ALTER TABLE public.window_activity OWNER TO postgres;

--
-- TOC entry 225 (class 1259 OID 16561)
-- Name: window_activity_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.window_activity_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.window_activity_id_seq OWNER TO postgres;

--
-- TOC entry 5177 (class 0 OID 0)
-- Dependencies: 225
-- Name: window_activity_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.window_activity_id_seq OWNED BY public.window_activity.id;


--
-- TOC entry 220 (class 1259 OID 16517)
-- Name: workstations; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.workstations (
    id integer NOT NULL,
    ip_address character varying(45) NOT NULL,
    username character varying(255) NOT NULL,
    domain_name character varying(255),
    host_name character varying(255) NOT NULL
);


ALTER TABLE public.workstations OWNER TO postgres;

--
-- TOC entry 219 (class 1259 OID 16516)
-- Name: workstations_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.workstations_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.workstations_id_seq OWNER TO postgres;

--
-- TOC entry 5178 (class 0 OID 0)
-- Dependencies: 219
-- Name: workstations_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.workstations_id_seq OWNED BY public.workstations.id;


--
-- TOC entry 4917 (class 2604 OID 16533)
-- Name: activity_events id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.activity_events ALTER COLUMN id SET DEFAULT nextval('public.activity_events_id_seq'::regclass);


--
-- TOC entry 4935 (class 2604 OID 32905)
-- Name: activity_period_types id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.activity_period_types ALTER COLUMN id SET DEFAULT nextval('public.activity_period_types_id_seq'::regclass);


--
-- TOC entry 4918 (class 2604 OID 16548)
-- Name: activity_periods id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.activity_periods ALTER COLUMN id SET DEFAULT nextval('public.activity_periods_id_seq'::regclass);


--
-- TOC entry 4929 (class 2604 OID 16738)
-- Name: application_rules id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.application_rules ALTER COLUMN id SET DEFAULT nextval('public.application_rules_id_seq'::regclass);


--
-- TOC entry 4921 (class 2604 OID 16604)
-- Name: dns_cache_records id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.dns_cache_records ALTER COLUMN id SET DEFAULT nextval('public.dns_cache_records_id_seq'::regclass);


--
-- TOC entry 4936 (class 2604 OID 41096)
-- Name: reports id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.reports ALTER COLUMN id SET DEFAULT nextval('public.reports_id_seq'::regclass);


--
-- TOC entry 4928 (class 2604 OID 16727)
-- Name: rule_types id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.rule_types ALTER COLUMN id SET DEFAULT nextval('public.rule_types_id_seq'::regclass);


--
-- TOC entry 4922 (class 2604 OID 16650)
-- Name: screenshots id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.screenshots ALTER COLUMN id SET DEFAULT nextval('public.screenshots_id_seq'::regclass);


--
-- TOC entry 4924 (class 2604 OID 16669)
-- Name: violations id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.violations ALTER COLUMN id SET DEFAULT nextval('public.violations_id_seq'::regclass);


--
-- TOC entry 4920 (class 2604 OID 16584)
-- Name: web_activity id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.web_activity ALTER COLUMN id SET DEFAULT nextval('public.web_activity_id_seq'::regclass);


--
-- TOC entry 4932 (class 2604 OID 16757)
-- Name: web_resource_rules id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.web_resource_rules ALTER COLUMN id SET DEFAULT nextval('public.web_resource_rules_id_seq'::regclass);


--
-- TOC entry 4919 (class 2604 OID 16565)
-- Name: window_activity id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.window_activity ALTER COLUMN id SET DEFAULT nextval('public.window_activity_id_seq'::regclass);


--
-- TOC entry 4916 (class 2604 OID 16520)
-- Name: workstations id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.workstations ALTER COLUMN id SET DEFAULT nextval('public.workstations_id_seq'::regclass);


--
-- TOC entry 5137 (class 0 OID 16530)
-- Dependencies: 222
-- Data for Name: activity_events; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.activity_events (id, workstation_id, last_active_time) FROM stdin;
\.


--
-- TOC entry 5157 (class 0 OID 32902)
-- Dependencies: 242
-- Data for Name: activity_period_types; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.activity_period_types (id, name) FROM stdin;
\.


--
-- TOC entry 5139 (class 0 OID 16545)
-- Dependencies: 224
-- Data for Name: activity_periods; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.activity_periods (id, workstation_id, start_time, end_time, duration_seconds, period_type_id) FROM stdin;
\.


--
-- TOC entry 5153 (class 0 OID 16735)
-- Dependencies: 238
-- Data for Name: application_rules; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.application_rules (id, application_name, rule_type_id, is_active, created_at) FROM stdin;
\.


--
-- TOC entry 5145 (class 0 OID 16601)
-- Dependencies: 230
-- Data for Name: dns_cache_records; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.dns_cache_records (id, workstation_id, domain_name, resolved_ip, record_time) FROM stdin;
\.


--
-- TOC entry 5159 (class 0 OID 41093)
-- Dependencies: 244
-- Data for Name: reports; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.reports (id, workstation_id, period_start, period_end, report_type, report_data, created_at) FROM stdin;
\.


--
-- TOC entry 5151 (class 0 OID 16724)
-- Dependencies: 236
-- Data for Name: rule_types; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.rule_types (id, name) FROM stdin;
\.


--
-- TOC entry 5147 (class 0 OID 16647)
-- Dependencies: 232
-- Data for Name: screenshots; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.screenshots (id, workstation_id, file_path, created_at) FROM stdin;
\.


--
-- TOC entry 5149 (class 0 OID 16666)
-- Dependencies: 234
-- Data for Name: violations; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.violations (id, workstation_id, source_table, source_event_id, violation_type, severity, description, related_entity, rule_source, detected_at, is_resolved, resolved_at, resolution_comment) FROM stdin;
\.


--
-- TOC entry 5143 (class 0 OID 16581)
-- Dependencies: 228
-- Data for Name: web_activity; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.web_activity (id, workstation_id, process_name, window_title, domain_name, detection_method, access_time) FROM stdin;
\.


--
-- TOC entry 5155 (class 0 OID 16754)
-- Dependencies: 240
-- Data for Name: web_resource_rules; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.web_resource_rules (id, domain_name, rule_type_id, is_active, created_at) FROM stdin;
\.


--
-- TOC entry 5141 (class 0 OID 16562)
-- Dependencies: 226
-- Data for Name: window_activity; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.window_activity (id, workstation_id, window_title, process_name, process_id, start_time, end_time, duration_seconds) FROM stdin;
\.


--
-- TOC entry 5135 (class 0 OID 16517)
-- Dependencies: 220
-- Data for Name: workstations; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.workstations (id, ip_address, username, domain_name, host_name) FROM stdin;
\.


--
-- TOC entry 5179 (class 0 OID 0)
-- Dependencies: 221
-- Name: activity_events_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.activity_events_id_seq', 12956, true);


--
-- TOC entry 5180 (class 0 OID 0)
-- Dependencies: 241
-- Name: activity_period_types_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.activity_period_types_id_seq', 2, true);


--
-- TOC entry 5181 (class 0 OID 0)
-- Dependencies: 223
-- Name: activity_periods_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.activity_periods_id_seq', 163, true);


--
-- TOC entry 5182 (class 0 OID 0)
-- Dependencies: 237
-- Name: application_rules_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.application_rules_id_seq', 12, true);


--
-- TOC entry 5183 (class 0 OID 0)
-- Dependencies: 229
-- Name: dns_cache_records_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.dns_cache_records_id_seq', 734, true);


--
-- TOC entry 5184 (class 0 OID 0)
-- Dependencies: 243
-- Name: reports_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.reports_id_seq', 2, true);


--
-- TOC entry 5185 (class 0 OID 0)
-- Dependencies: 235
-- Name: rule_types_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.rule_types_id_seq', 33, true);


--
-- TOC entry 5186 (class 0 OID 0)
-- Dependencies: 231
-- Name: screenshots_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.screenshots_id_seq', 5, true);


--
-- TOC entry 5187 (class 0 OID 0)
-- Dependencies: 233
-- Name: violations_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.violations_id_seq', 72, true);


--
-- TOC entry 5188 (class 0 OID 0)
-- Dependencies: 227
-- Name: web_activity_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.web_activity_id_seq', 940, true);


--
-- TOC entry 5189 (class 0 OID 0)
-- Dependencies: 239
-- Name: web_resource_rules_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.web_resource_rules_id_seq', 6, true);


--
-- TOC entry 5190 (class 0 OID 0)
-- Dependencies: 225
-- Name: window_activity_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.window_activity_id_seq', 269, true);


--
-- TOC entry 5191 (class 0 OID 0)
-- Dependencies: 219
-- Name: workstations_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.workstations_id_seq', 752, true);


--
-- TOC entry 4947 (class 2606 OID 16538)
-- Name: activity_events activity_events_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.activity_events
    ADD CONSTRAINT activity_events_pkey PRIMARY KEY (id);


--
-- TOC entry 4971 (class 2606 OID 32911)
-- Name: activity_period_types activity_period_types_name_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.activity_period_types
    ADD CONSTRAINT activity_period_types_name_key UNIQUE (name);


--
-- TOC entry 4973 (class 2606 OID 32909)
-- Name: activity_period_types activity_period_types_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.activity_period_types
    ADD CONSTRAINT activity_period_types_pkey PRIMARY KEY (id);


--
-- TOC entry 4951 (class 2606 OID 16555)
-- Name: activity_periods activity_periods_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.activity_periods
    ADD CONSTRAINT activity_periods_pkey PRIMARY KEY (id);


--
-- TOC entry 4967 (class 2606 OID 16747)
-- Name: application_rules application_rules_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.application_rules
    ADD CONSTRAINT application_rules_pkey PRIMARY KEY (id);


--
-- TOC entry 4957 (class 2606 OID 16610)
-- Name: dns_cache_records dns_cache_records_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.dns_cache_records
    ADD CONSTRAINT dns_cache_records_pkey PRIMARY KEY (id);


--
-- TOC entry 4975 (class 2606 OID 41109)
-- Name: reports reports_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.reports
    ADD CONSTRAINT reports_pkey PRIMARY KEY (id);


--
-- TOC entry 4963 (class 2606 OID 16733)
-- Name: rule_types rule_types_name_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.rule_types
    ADD CONSTRAINT rule_types_name_key UNIQUE (name);


--
-- TOC entry 4965 (class 2606 OID 16731)
-- Name: rule_types rule_types_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.rule_types
    ADD CONSTRAINT rule_types_pkey PRIMARY KEY (id);


--
-- TOC entry 4959 (class 2606 OID 16659)
-- Name: screenshots screenshots_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.screenshots
    ADD CONSTRAINT screenshots_pkey PRIMARY KEY (id);


--
-- TOC entry 4949 (class 2606 OID 32900)
-- Name: activity_events unique_activity_workstation; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.activity_events
    ADD CONSTRAINT unique_activity_workstation UNIQUE (workstation_id);


--
-- TOC entry 4943 (class 2606 OID 24708)
-- Name: workstations unique_workstation_ip; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.workstations
    ADD CONSTRAINT unique_workstation_ip UNIQUE (ip_address);


--
-- TOC entry 4961 (class 2606 OID 16685)
-- Name: violations violations_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.violations
    ADD CONSTRAINT violations_pkey PRIMARY KEY (id);


--
-- TOC entry 4955 (class 2606 OID 16594)
-- Name: web_activity web_activity_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.web_activity
    ADD CONSTRAINT web_activity_pkey PRIMARY KEY (id);


--
-- TOC entry 4969 (class 2606 OID 16766)
-- Name: web_resource_rules web_resource_rules_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.web_resource_rules
    ADD CONSTRAINT web_resource_rules_pkey PRIMARY KEY (id);


--
-- TOC entry 4953 (class 2606 OID 16574)
-- Name: window_activity window_activity_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.window_activity
    ADD CONSTRAINT window_activity_pkey PRIMARY KEY (id);


--
-- TOC entry 4945 (class 2606 OID 16528)
-- Name: workstations workstations_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.workstations
    ADD CONSTRAINT workstations_pkey PRIMARY KEY (id);


--
-- TOC entry 4976 (class 2606 OID 16539)
-- Name: activity_events activity_events_workstation_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.activity_events
    ADD CONSTRAINT activity_events_workstation_id_fkey FOREIGN KEY (workstation_id) REFERENCES public.workstations(id) ON DELETE CASCADE;


--
-- TOC entry 4977 (class 2606 OID 16556)
-- Name: activity_periods activity_periods_workstation_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.activity_periods
    ADD CONSTRAINT activity_periods_workstation_id_fkey FOREIGN KEY (workstation_id) REFERENCES public.workstations(id) ON DELETE CASCADE;


--
-- TOC entry 4984 (class 2606 OID 16748)
-- Name: application_rules application_rules_rule_type_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.application_rules
    ADD CONSTRAINT application_rules_rule_type_id_fkey FOREIGN KEY (rule_type_id) REFERENCES public.rule_types(id);


--
-- TOC entry 4981 (class 2606 OID 16611)
-- Name: dns_cache_records dns_cache_records_workstation_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.dns_cache_records
    ADD CONSTRAINT dns_cache_records_workstation_id_fkey FOREIGN KEY (workstation_id) REFERENCES public.workstations(id) ON DELETE CASCADE;


--
-- TOC entry 4978 (class 2606 OID 32913)
-- Name: activity_periods fk_activity_period_type; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.activity_periods
    ADD CONSTRAINT fk_activity_period_type FOREIGN KEY (period_type_id) REFERENCES public.activity_period_types(id);


--
-- TOC entry 4986 (class 2606 OID 41110)
-- Name: reports reports_workstation_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.reports
    ADD CONSTRAINT reports_workstation_id_fkey FOREIGN KEY (workstation_id) REFERENCES public.workstations(id) ON DELETE CASCADE;


--
-- TOC entry 4982 (class 2606 OID 16660)
-- Name: screenshots screenshots_workstation_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.screenshots
    ADD CONSTRAINT screenshots_workstation_id_fkey FOREIGN KEY (workstation_id) REFERENCES public.workstations(id) ON DELETE CASCADE;


--
-- TOC entry 4983 (class 2606 OID 16686)
-- Name: violations violations_workstation_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.violations
    ADD CONSTRAINT violations_workstation_id_fkey FOREIGN KEY (workstation_id) REFERENCES public.workstations(id) ON DELETE CASCADE;


--
-- TOC entry 4980 (class 2606 OID 16595)
-- Name: web_activity web_activity_workstation_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.web_activity
    ADD CONSTRAINT web_activity_workstation_id_fkey FOREIGN KEY (workstation_id) REFERENCES public.workstations(id) ON DELETE CASCADE;


--
-- TOC entry 4985 (class 2606 OID 16767)
-- Name: web_resource_rules web_resource_rules_rule_type_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.web_resource_rules
    ADD CONSTRAINT web_resource_rules_rule_type_id_fkey FOREIGN KEY (rule_type_id) REFERENCES public.rule_types(id);


--
-- TOC entry 4979 (class 2606 OID 16575)
-- Name: window_activity window_activity_workstation_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.window_activity
    ADD CONSTRAINT window_activity_workstation_id_fkey FOREIGN KEY (workstation_id) REFERENCES public.workstations(id) ON DELETE CASCADE;


-- Completed on 2026-05-06 01:43:10

--
-- PostgreSQL database dump complete
--

\unrestrict xA6NWBTv1atYBXtYFtwnMgM01Vb9q8J6xndrqrvLlSuOzzuuaxIqzAMEKaTIGlb

